using System.Globalization;
using System.Text.RegularExpressions;
using DiarioX.Server.Application.DTOs.Faturamento;
using DiarioX.Server.Application.Faturamento;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiarioX.Server.Application.Services;

public class FaturamentoService : IFaturamentoService
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> SituacoesAssinatura =
        [Assinatura.SituacaoTeste, Assinatura.SituacaoAtiva, Assinatura.SituacaoCancelada];

    private readonly IFaturamentoRepository _repository;
    private readonly IAsaasClient _asaas;
    private readonly FaturamentoOptions _options;
    private readonly ILogger<FaturamentoService> _logger;

    public FaturamentoService(
        IFaturamentoRepository repository,
        IAsaasClient asaas,
        IOptions<FaturamentoOptions> options,
        ILogger<FaturamentoService> logger)
    {
        _repository = repository;
        _asaas = asaas;
        _options = options.Value;
        _logger = logger;
    }

    // ---------- Painel ----------

    public async Task<PainelFaturamentoResponse> ObterPainelAsync(DateOnly hoje)
    {
        var assinaturas = await _repository.ListarAssinaturasAsync();
        var faturas = await _repository.ListarFaturasAsync();
        var instituicoes = await _repository.ListarInstituicoesAsync();
        var alunos = await _repository.ContarAlunosAtivosAsync(hoje);
        var competenciaAtual = Competencia(hoje);

        var ativas = assinaturas.Where(a => a.Situacao == Assinatura.SituacaoAtiva).ToList();

        return new PainelFaturamentoResponse(
            _asaas.Configurado,
            _options.NotaFiscal.Habilitada,
            _options.DiasAntecedenciaFatura,
            _options.DiasAtrasoSomenteLeitura,
            ativas.Count,
            assinaturas.Count(a => a.Situacao == Assinatura.SituacaoTeste),
            ativas.Sum(a => ValorComDesconto(a.Plano.CalcularValor(alunos.GetValueOrDefault(a.TenantId)), a.DescontoPercentual)),
            faturas.Where(f => f.Competencia == competenciaAtual && f.Situacao != FaturaAssinatura.SituacaoCancelada).Sum(f => f.Valor),
            faturas.Where(f => f.PagaEm is DateOnly d && Competencia(d) == competenciaAtual).Sum(f => f.ValorPago ?? 0),
            faturas.Where(f => f.Situacao == FaturaAssinatura.SituacaoPendente).Sum(f => f.Valor),
            faturas.Where(f => f.Situacao == FaturaAssinatura.SituacaoVencida).Sum(f => f.Valor),
            instituicoes.Count(t => t.SituacaoFinanceira == Tenant.SituacaoFinanceiraEmAtraso),
            instituicoes.Count(t => t.SituacaoFinanceira == Tenant.SituacaoFinanceiraSomenteLeitura),
            faturas.Where(f => f.EmAberto && f.Vencimento < hoje)
                .OrderBy(f => f.Vencimento)
                .Select(f => Map(f, hoje))
                .ToList());
    }

    // ---------- Planos ----------

    public async Task<IEnumerable<PlanoResponse>> ListarPlanosAsync()
    {
        var planos = await _repository.ListarPlanosAsync();
        var uso = (await _repository.ListarAssinaturasAsync())
            .Where(a => a.Situacao != Assinatura.SituacaoCancelada)
            .GroupBy(a => a.PlanoId)
            .ToDictionary(g => g.Key, g => g.Count());

        return planos.Select(p => MapPlano(p, uso.GetValueOrDefault(p.Id)));
    }

    public async Task<FaturamentoResult<PlanoResponse>> SalvarPlanoAsync(int? id, PlanoRequest request)
    {
        var nome = (request.Nome ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(nome))
            return Invalid<PlanoResponse>("Informe o nome do plano.");
        if (nome.Length > 100)
            return Invalid<PlanoResponse>("O nome do plano deve ter no máximo 100 caracteres.");
        if (request.ValorFixo < 0 || request.ValorPorAluno < 0 || request.ValorMinimo < 0)
            return Invalid<PlanoResponse>("Os valores do plano não podem ser negativos.");
        if ((request.Descricao?.Trim().Length ?? 0) > 500)
            return Invalid<PlanoResponse>("A descrição deve ter no máximo 500 caracteres.");

        if (await _repository.ExistePlanoComNomeAsync(nome, id))
            return new(default, "Já existe um plano com este nome.", FaturamentoErro.Conflict);

        PlanoAssinatura plano;
        if (id is null)
        {
            plano = new PlanoAssinatura();
            _repository.Adicionar(plano);
        }
        else
        {
            var existente = await _repository.ObterPlanoAsync(id.Value);
            if (existente is null)
                return new(default, "Plano não encontrado.", FaturamentoErro.NotFound);
            plano = existente;
            plano.UpdatedAt = DateTime.UtcNow;
        }

        plano.Nome = nome;
        plano.Descricao = string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao.Trim();
        plano.ValorFixo = Math.Round(request.ValorFixo, 2);
        plano.ValorPorAluno = Math.Round(request.ValorPorAluno, 2);
        plano.ValorMinimo = Math.Round(request.ValorMinimo, 2);
        plano.Ativo = request.Ativo;
        await _repository.SalvarAsync();

        return new(MapPlano(plano, 0), id is null ? "Plano cadastrado com sucesso!" : "Plano atualizado com sucesso!");
    }

    // ---------- Assinaturas ----------

    public async Task<IEnumerable<AssinaturaResumoResponse>> ListarAssinaturasAsync(DateOnly hoje)
    {
        var instituicoes = await _repository.ListarInstituicoesAsync();
        var assinaturas = (await _repository.ListarAssinaturasAsync()).ToDictionary(a => a.TenantId);
        var abertas = (await _repository.ListarFaturasAsync()).Where(f => f.EmAberto).ToLookup(f => f.TenantId);
        var alunos = await _repository.ContarAlunosAtivosAsync(hoje);

        return instituicoes.Select(t =>
        {
            assinaturas.TryGetValue(t.Id, out var a);
            var quantidade = alunos.GetValueOrDefault(t.Id);
            return new AssinaturaResumoResponse(
                t.Id, t.Nome, t.Slug, t.Status, t.SituacaoFinanceira,
                a?.Id, a?.Plano.Nome, a?.Situacao, a?.DiaVencimento, a?.TesteAte,
                quantidade,
                a is null ? null : ValorComDesconto(a.Plano.CalcularValor(quantidade), a.DescontoPercentual),
                abertas[t.Id].Count(),
                abertas[t.Id].Sum(f => f.Valor));
        });
    }

    public async Task<FaturamentoResult<AssinaturaResponse>> ObterAssinaturaAsync(int tenantId)
    {
        var assinatura = await _repository.ObterAssinaturaPorInstituicaoAsync(tenantId);
        return assinatura is null
            ? new(default, "A instituição ainda não tem assinatura.", FaturamentoErro.NotFound)
            : new(MapAssinatura(assinatura));
    }

    public async Task<FaturamentoResult<AssinaturaResponse>> SalvarAssinaturaAsync(int tenantId, AssinaturaRequest request)
    {
        var instituicao = await _repository.ObterInstituicaoAsync(tenantId);
        if (instituicao is null)
            return new(default, "Instituição não encontrada.", FaturamentoErro.NotFound);

        var assinatura = await _repository.ObterAssinaturaPorInstituicaoAsync(tenantId);

        var plano = await _repository.ObterPlanoAsync(request.PlanoId);
        if (plano is null)
            return Invalid<AssinaturaResponse>("Selecione um plano válido.");
        if (!plano.Ativo && assinatura?.PlanoId != plano.Id)
            return Invalid<AssinaturaResponse>("O plano selecionado está inativo.");

        var situacao = (request.Situacao ?? string.Empty).Trim().ToUpperInvariant();
        if (!SituacoesAssinatura.Contains(situacao))
            return Invalid<AssinaturaResponse>("Situação inválida. Use Teste, Ativa ou Cancelada.");
        if (request.DataInicio == default)
            return Invalid<AssinaturaResponse>("Informe a data de início da assinatura.");
        if (situacao == Assinatura.SituacaoTeste && request.TesteAte is null)
            return Invalid<AssinaturaResponse>("Informe até quando vai o período de teste.");
        if (request.TesteAte is DateOnly teste && teste < request.DataInicio)
            return Invalid<AssinaturaResponse>("O fim do teste não pode ser anterior ao início da assinatura.");
        if (request.DiaVencimento < 1 || request.DiaVencimento > Assinatura.DiaVencimentoMaximo)
            return Invalid<AssinaturaResponse>($"O dia de vencimento deve estar entre 1 e {Assinatura.DiaVencimentoMaximo}.");
        if (request.DescontoPercentual < 0 || request.DescontoPercentual > 100)
            return Invalid<AssinaturaResponse>("O desconto deve estar entre 0% e 100%.");

        var razaoSocial = (request.RazaoSocial ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(razaoSocial))
            return Invalid<AssinaturaResponse>("Informe a razão social ou o nome do responsável pelo pagamento.");

        var documento = Documentos.SomenteDigitos(request.CpfCnpj);
        if (!Documentos.CpfOuCnpjValido(documento))
            return Invalid<AssinaturaResponse>("Informe um CPF ou CNPJ válido para a cobrança e a nota fiscal.");

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (!EmailPattern.IsMatch(email))
            return Invalid<AssinaturaResponse>("Informe um e-mail válido para o envio das faturas.");

        var cep = Documentos.SomenteDigitos(request.Cep);
        if (cep.Length is > 0 and not 8)
            return Invalid<AssinaturaResponse>("O CEP deve ter 8 dígitos.");

        if (assinatura is null)
        {
            assinatura = new Assinatura { TenantId = tenantId };
            _repository.Adicionar(assinatura);
        }
        else
        {
            assinatura.UpdatedAt = DateTime.UtcNow;
        }

        assinatura.PlanoId = plano.Id;
        assinatura.Plano = plano;
        assinatura.Tenant = instituicao;
        assinatura.Situacao = situacao;
        assinatura.DataInicio = request.DataInicio;
        assinatura.TesteAte = request.TesteAte;
        assinatura.DiaVencimento = request.DiaVencimento;
        assinatura.DescontoPercentual = Math.Round(request.DescontoPercentual, 2);
        assinatura.RazaoSocial = razaoSocial;
        assinatura.CpfCnpj = documento;
        assinatura.Email = email;
        assinatura.Telefone = Opcional(Documentos.SomenteDigitos(request.Telefone));
        assinatura.Cep = Opcional(cep);
        assinatura.Endereco = Opcional(request.Endereco);
        assinatura.Numero = Opcional(request.Numero);
        assinatura.Complemento = Opcional(request.Complemento);
        assinatura.Bairro = Opcional(request.Bairro);
        await _repository.SalvarAsync();

        // Os dados do tomador vão para o Asaas na próxima fatura.
        return new(MapAssinatura(assinatura), "Assinatura salva com sucesso!");
    }

    // ---------- Faturas ----------

    public async Task<IEnumerable<FaturaResponse>> ListarFaturasAsync(int? tenantId, string? situacao, DateOnly hoje)
        => (await _repository.ListarFaturasAsync(tenantId, situacao?.Trim().ToUpperInvariant())).Select(f => Map(f, hoje));

    public async Task<FaturamentoResult<FaturaResponse>> GerarFaturaAsync(int tenantId, GerarFaturaRequest request, DateOnly hoje)
    {
        var assinatura = await _repository.ObterAssinaturaPorInstituicaoAsync(tenantId);
        if (assinatura is null)
            return new(default, "A instituição ainda não tem assinatura.", FaturamentoErro.NotFound);
        if (assinatura.Situacao == Assinatura.SituacaoCancelada)
            return Invalid<FaturaResponse>("A assinatura está cancelada. Reative-a antes de gerar faturas.");

        var vencimento = request.Vencimento ?? ProximoVencimento(hoje, assinatura.DiaVencimento);
        if (vencimento < hoje)
            return Invalid<FaturaResponse>("O vencimento deve ser hoje ou uma data futura.");

        if (await _repository.ExisteFaturaAsync(assinatura.Id, Competencia(vencimento), incluirCanceladas: false))
        {
            return new(default,
                $"Já existe uma fatura para a competência {Competencia(vencimento):MM/yyyy}. Cancele-a para gerar outra.",
                FaturamentoErro.Conflict);
        }

        var alunos = (await _repository.ContarAlunosAtivosAsync(hoje)).GetValueOrDefault(tenantId);
        var fatura = await CriarFaturaAsync(assinatura, vencimento, alunos, hoje);

        var mensagem = fatura.Observacao is null ? "Fatura gerada com sucesso!" : $"Fatura gerada. {fatura.Observacao}";
        return new(Map(fatura, hoje), mensagem);
    }

    public async Task<FaturamentoResult<FaturaResponse>> RegistrarPagamentoManualAsync(int faturaId, PagamentoManualRequest request, DateOnly hoje)
    {
        var fatura = await _repository.ObterFaturaAsync(faturaId);
        if (fatura is null)
            return new(default, "Fatura não encontrada.", FaturamentoErro.NotFound);
        if (!fatura.EmAberto)
            return Invalid<FaturaResponse>("Só é possível registrar o pagamento de uma fatura em aberto.");
        if (request.Data == default)
            return Invalid<FaturaResponse>("Informe a data do pagamento.");
        if (request.Data > hoje)
            return Invalid<FaturaResponse>("A data do pagamento não pode ser futura.");

        var valor = request.Valor ?? fatura.Valor;
        if (valor <= 0)
            return Invalid<FaturaResponse>("Informe o valor recebido.");

        // Com cobrança no Asaas, a baixa é feita lá também para não cobrar o cliente de novo.
        if (fatura.AsaasCobrancaId is not null && _asaas.Configurado)
        {
            try
            {
                await _asaas.ConfirmarRecebimentoEmDinheiroAsync(fatura.AsaasCobrancaId, request.Data, valor);
            }
            catch (AsaasException ex)
            {
                return new(default, ex.Message, FaturamentoErro.Integracao);
            }
        }

        MarcarComoPaga(fatura, request.Data, valor, "MANUAL");
        await AplicarReguaAsync(hoje, fatura.TenantId);
        await _repository.SalvarAsync();

        var erroNota = await TentarEmitirNotaAsync(fatura, hoje);
        await _repository.SalvarAsync();

        return new(Map(fatura, hoje), erroNota is null
            ? "Pagamento registrado com sucesso!"
            : $"Pagamento registrado, mas a nota fiscal não foi emitida: {erroNota}");
    }

    public async Task<FaturamentoResult<FaturaResponse>> CancelarFaturaAsync(int faturaId, DateOnly hoje)
    {
        var fatura = await _repository.ObterFaturaAsync(faturaId);
        if (fatura is null)
            return new(default, "Fatura não encontrada.", FaturamentoErro.NotFound);
        if (!fatura.EmAberto)
            return Invalid<FaturaResponse>("Só faturas em aberto podem ser canceladas.");

        if (fatura.AsaasCobrancaId is not null && _asaas.Configurado)
        {
            try
            {
                await _asaas.CancelarCobrancaAsync(fatura.AsaasCobrancaId);
            }
            catch (AsaasException ex)
            {
                return new(default, ex.Message, FaturamentoErro.Integracao);
            }
        }

        fatura.Situacao = FaturaAssinatura.SituacaoCancelada;
        fatura.UpdatedAt = DateTime.UtcNow;
        await AplicarReguaAsync(hoje, fatura.TenantId);
        await _repository.SalvarAsync();

        return new(Map(fatura, hoje), "Fatura cancelada com sucesso!");
    }

    public async Task<FaturamentoResult<FaturaResponse>> EmitirNotaFiscalAsync(int faturaId, DateOnly hoje)
    {
        var fatura = await _repository.ObterFaturaAsync(faturaId);
        if (fatura is null)
            return new(default, "Fatura não encontrada.", FaturamentoErro.NotFound);
        if (!_options.NotaFiscal.Habilitada)
            return Invalid<FaturaResponse>("A emissão de nota fiscal está desativada (Faturamento:NotaFiscal:Habilitada).");
        if (fatura.Situacao != FaturaAssinatura.SituacaoPaga)
            return Invalid<FaturaResponse>("A nota fiscal é emitida para faturas pagas.");
        if (fatura.AsaasCobrancaId is null)
            return Invalid<FaturaResponse>("A fatura não tem cobrança no Asaas. Emita a nota pelo painel do Asaas.");
        if (fatura.NotaFiscalSituacao is FaturaAssinatura.NotaAgendada or FaturaAssinatura.NotaEmitida)
            return new(default, "A nota fiscal desta fatura já foi solicitada.", FaturamentoErro.Conflict);

        var erro = await TentarEmitirNotaAsync(fatura, hoje, forcar: true);
        await _repository.SalvarAsync();

        return erro is null
            ? new(Map(fatura, hoje), "Nota fiscal solicitada ao Asaas. O número e o PDF chegam pelo webhook.")
            : new(default, erro, FaturamentoErro.Integracao);
    }

    // ---------- Rotina ----------

    public async Task<RotinaFaturamentoResponse> ExecutarRotinaAsync(DateOnly hoje, CancellationToken ct = default)
    {
        var erros = new List<string>();
        var geradas = new HashSet<int>();
        var alunos = await _repository.ContarAlunosAtivosAsync(hoje);

        foreach (var assinatura in await _repository.ListarAssinaturasAsync())
        {
            ct.ThrowIfCancellationRequested();

            if (assinatura.Situacao == Assinatura.SituacaoTeste && assinatura.TesteAte < hoje)
            {
                assinatura.Situacao = Assinatura.SituacaoAtiva;
                assinatura.UpdatedAt = DateTime.UtcNow;
            }

            if (assinatura.Situacao == Assinatura.SituacaoCancelada)
                continue;

            var vencimento = ProximoVencimento(hoje, assinatura.DiaVencimento);
            var foraDaJanela = hoje < vencimento.AddDays(-_options.DiasAntecedenciaFatura);
            var antesDoInicio = vencimento < assinatura.DataInicio;
            var emTeste = assinatura.TesteAte is DateOnly fimTeste && vencimento <= fimTeste;
            if (foraDaJanela || antesDoInicio || emTeste)
                continue;

            // Canceladas contam: a rotina não recria uma fatura que o administrador cancelou.
            if (await _repository.ExisteFaturaAsync(assinatura.Id, Competencia(vencimento), incluirCanceladas: true))
                continue;

            try
            {
                var fatura = await CriarFaturaAsync(assinatura, vencimento, alunos.GetValueOrDefault(assinatura.TenantId), hoje);
                geradas.Add(fatura.Id);
                if (fatura.Valor > 0 && fatura.AsaasCobrancaId is null && _asaas.Configurado)
                    erros.Add($"{assinatura.Tenant.Nome}: {fatura.Observacao}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Erro ao gerar a fatura da instituição {TenantId}", assinatura.TenantId);
                erros.Add($"{assinatura.Tenant.Nome}: {ex.Message}");
            }
        }

        // Faturas de execuções anteriores que ficaram sem cobrança (Asaas fora do ar ou sem chave).
        var reenviadas = 0;
        if (_asaas.Configurado)
        {
            var semCobranca = (await _repository.ListarFaturasAsync())
                .Where(f => f.EmAberto && f.AsaasCobrancaId is null && f.Valor > 0 && f.Vencimento >= hoje)
                .Where(f => !geradas.Contains(f.Id));
            foreach (var fatura in semCobranca)
            {
                if (await EnviarCobrancaAsync(fatura))
                    reenviadas++;
                else
                    erros.Add($"{fatura.Assinatura.Tenant.Nome}: {fatura.Observacao}");
            }
        }

        var (vencidas, atualizadas) = await AplicarReguaAsync(hoje);
        await _repository.SalvarAsync();

        return new RotinaFaturamentoResponse(geradas.Count, reenviadas, vencidas, atualizadas, erros);
    }

    // ---------- Webhook ----------

    public async Task ProcessarEventoAsync(AsaasEvento evento, DateOnly hoje)
    {
        if (evento.Evento.StartsWith("INVOICE_", StringComparison.Ordinal))
        {
            await ProcessarNotaFiscalAsync(evento.NotaFiscal);
            return;
        }

        if (evento.Cobranca is null)
            return;

        var fatura = await LocalizarFaturaAsync(evento.Cobranca);
        if (fatura is null)
        {
            _logger.LogInformation("Evento {Evento} do Asaas sem fatura correspondente (cobrança {Cobranca})",
                evento.Evento, evento.Cobranca.Id);
            return;
        }

        switch (evento.Evento)
        {
            case "PAYMENT_RECEIVED" or "PAYMENT_CONFIRMED":
                if (fatura.Situacao == FaturaAssinatura.SituacaoPaga)
                    return;

                MarcarComoPaga(fatura, evento.Cobranca.DataPagamento ?? hoje, evento.Cobranca.Valor ?? fatura.Valor,
                    evento.Cobranca.FormaPagamento);
                await AplicarReguaAsync(hoje, fatura.TenantId);
                await _repository.SalvarAsync();

                await TentarEmitirNotaAsync(fatura, hoje);
                break;

            case "PAYMENT_OVERDUE":
                if (fatura.Situacao == FaturaAssinatura.SituacaoPendente)
                    fatura.Situacao = FaturaAssinatura.SituacaoVencida;
                break;

            case "PAYMENT_DELETED":
                if (fatura.EmAberto)
                    fatura.Situacao = FaturaAssinatura.SituacaoCancelada;
                break;

            case "PAYMENT_RESTORED":
                if (fatura.Situacao == FaturaAssinatura.SituacaoCancelada)
                    fatura.Situacao = fatura.Vencimento < hoje ? FaturaAssinatura.SituacaoVencida : FaturaAssinatura.SituacaoPendente;
                break;

            case "PAYMENT_REFUNDED":
                if (fatura.Situacao == FaturaAssinatura.SituacaoPaga)
                    fatura.Situacao = FaturaAssinatura.SituacaoEstornada;
                break;

            default:
                return;
        }

        fatura.UpdatedAt = DateTime.UtcNow;
        await AplicarReguaAsync(hoje, fatura.TenantId);
        await _repository.SalvarAsync();
    }

    // ---------- Área da instituição ----------

    public async Task<AvisoFinanceiroResponse> ObterAvisoAsync(int tenantId, bool podeVerFaturas, DateOnly hoje)
    {
        var instituicao = await _repository.ObterInstituicaoAsync(tenantId);
        var maisAntiga = (await _repository.ListarFaturasAsync(tenantId))
            .Where(f => f.EmAberto && f.Vencimento < hoje)
            .Select(f => (DateOnly?)f.Vencimento)
            .Min();

        return new AvisoFinanceiroResponse(
            instituicao?.SituacaoFinanceira ?? Tenant.SituacaoFinanceiraRegular,
            maisAntiga is DateOnly v ? hoje.DayNumber - v.DayNumber : 0,
            maisAntiga,
            maisAntiga?.AddDays(_options.DiasAtrasoSomenteLeitura),
            podeVerFaturas);
    }

    public async Task<MinhaAssinaturaResponse?> ObterMinhaAssinaturaAsync(int tenantId)
    {
        var assinatura = await _repository.ObterAssinaturaPorInstituicaoAsync(tenantId);
        if (assinatura is null)
            return null;

        var faturas = (await _repository.ListarFaturasAsync(tenantId))
            .Where(f => f.Situacao != FaturaAssinatura.SituacaoCancelada)
            .Select(f => new MinhaFaturaResponse(
                f.Id, f.Competencia, f.Vencimento, f.AlunosAtivos, f.Valor, f.Situacao,
                f.EmAberto ? f.LinkPagamento : null, f.PagaEm, f.NotaFiscalNumero, f.NotaFiscalPdfUrl))
            .ToList();

        return new MinhaAssinaturaResponse(
            assinatura.Plano.Nome, assinatura.Situacao, assinatura.DiaVencimento, assinatura.TesteAte,
            assinatura.Tenant.SituacaoFinanceira, faturas);
    }

    // ---------- Regras internas ----------

    private async Task<FaturaAssinatura> CriarFaturaAsync(Assinatura assinatura, DateOnly vencimento, int alunos, DateOnly hoje)
    {
        var plano = assinatura.Plano;
        var calculado = plano.CalcularValor(alunos);
        var valor = ValorComDesconto(calculado, assinatura.DescontoPercentual);

        var fatura = new FaturaAssinatura
        {
            AssinaturaId = assinatura.Id,
            Assinatura = assinatura,
            TenantId = assinatura.TenantId,
            Competencia = Competencia(vencimento),
            Vencimento = vencimento,
            AlunosAtivos = alunos,
            ValorFixo = plano.ValorFixo,
            ValorPorAluno = plano.ValorPorAluno,
            ValorMinimo = plano.ValorMinimo,
            ValorCalculado = calculado,
            DescontoPercentual = assinatura.DescontoPercentual,
            Valor = valor,
        };

        if (valor <= 0)
        {
            MarcarComoPaga(fatura, hoje, 0, null);
            fatura.Observacao = "Fatura sem valor a cobrar (plano ou desconto).";
        }

        _repository.Adicionar(fatura);
        await _repository.SalvarAsync(); // gera o Id usado como referência externa no Asaas

        if (valor > 0)
        {
            await EnviarCobrancaAsync(fatura);
            await _repository.SalvarAsync();
        }

        return fatura;
    }

    private async Task<bool> EnviarCobrancaAsync(FaturaAssinatura fatura)
    {
        if (!_asaas.Configurado)
        {
            fatura.Observacao = "Asaas não configurado: a cobrança não foi enviada. Registre o pagamento manualmente.";
            return false;
        }

        var assinatura = fatura.Assinatura;
        try
        {
            assinatura.AsaasClienteId = await _asaas.SalvarClienteAsync(assinatura.AsaasClienteId, new AsaasCliente(
                assinatura.RazaoSocial, assinatura.CpfCnpj, assinatura.Email, assinatura.Telefone, assinatura.Cep,
                assinatura.Endereco, assinatura.Numero, assinatura.Complemento, assinatura.Bairro,
                $"tenant-{assinatura.TenantId}"));

            var cobranca = await _asaas.CriarCobrancaAsync(new AsaasNovaCobranca(
                assinatura.AsaasClienteId,
                fatura.Valor,
                fatura.Vencimento,
                DescricaoCobranca(fatura),
                ReferenciaFatura(fatura),
                _options.MultaPercentual,
                _options.JurosMensalPercentual));

            fatura.AsaasCobrancaId = cobranca.Id;
            fatura.LinkPagamento = cobranca.LinkPagamento;
            fatura.Observacao = null;
            fatura.UpdatedAt = DateTime.UtcNow;
            return true;
        }
        catch (AsaasException ex)
        {
            _logger.LogWarning(ex, "Cobrança da fatura {FaturaId} não enviada ao Asaas", fatura.Id);
            fatura.Observacao = Truncar($"Cobrança não enviada ao Asaas: {ex.Message}", 500);
            return false;
        }
    }

    /// <summary>Solicita a NFS-e da fatura paga. Devolve a mensagem de erro, ou nulo se deu certo ou não se aplica.</summary>
    private async Task<string?> TentarEmitirNotaAsync(FaturaAssinatura fatura, DateOnly hoje, bool forcar = false)
    {
        var nota = _options.NotaFiscal;
        if (!nota.Habilitada || !_asaas.Configurado || fatura.AsaasCobrancaId is null || fatura.Valor <= 0)
            return null;

        var jaSolicitada = fatura.NotaFiscalSituacao is FaturaAssinatura.NotaAgendada or FaturaAssinatura.NotaEmitida;
        if (jaSolicitada || (fatura.NotaFiscalSituacao is not null && !forcar))
            return null;

        try
        {
            var agendada = await _asaas.AgendarNotaFiscalAsync(new AsaasNovaNotaFiscal(
                fatura.AsaasCobrancaId,
                fatura.Valor,
                DescricaoServico(fatura),
                ReferenciaFatura(fatura),
                hoje,
                nota.MunicipalServiceId,
                nota.MunicipalServiceCode,
                nota.MunicipalServiceName,
                nota.AliquotaIss,
                nota.RetemIss));

            fatura.NotaFiscalId = agendada.Id;
            fatura.NotaFiscalSituacao = SituacaoNota(agendada.Status) ?? FaturaAssinatura.NotaAgendada;
            fatura.NotaFiscalErro = null;
            return null;
        }
        catch (AsaasException ex)
        {
            _logger.LogWarning(ex, "NFS-e da fatura {FaturaId} não solicitada", fatura.Id);
            fatura.NotaFiscalSituacao = FaturaAssinatura.NotaErro;
            fatura.NotaFiscalErro = Truncar(ex.Message, 500);
            return ex.Message;
        }
    }

    private async Task ProcessarNotaFiscalAsync(AsaasEventoNotaFiscal? nota)
    {
        if (nota is null)
            return;

        var fatura = await _repository.ObterFaturaPorNotaFiscalAsync(nota.Id);
        if (fatura is null && nota.CobrancaId is not null)
            fatura = await _repository.ObterFaturaPorCobrancaAsync(nota.CobrancaId);
        if (fatura is null)
            return;

        fatura.NotaFiscalId ??= nota.Id;
        var situacao = SituacaoNota(nota.Status);
        if (situacao is not null)
            fatura.NotaFiscalSituacao = situacao;

        if (situacao == FaturaAssinatura.NotaEmitida)
        {
            fatura.NotaFiscalNumero = nota.Numero ?? fatura.NotaFiscalNumero;
            fatura.NotaFiscalPdfUrl = nota.PdfUrl ?? fatura.NotaFiscalPdfUrl;
            fatura.NotaFiscalErro = null;
        }
        else if (situacao == FaturaAssinatura.NotaErro)
        {
            fatura.NotaFiscalErro = Truncar(nota.Descricao ?? "Erro na emissão. Veja os detalhes no painel do Asaas.", 500);
        }

        fatura.UpdatedAt = DateTime.UtcNow;
        await _repository.SalvarAsync();
    }

    private async Task<FaturaAssinatura?> LocalizarFaturaAsync(AsaasEventoCobranca cobranca)
    {
        var fatura = await _repository.ObterFaturaPorCobrancaAsync(cobranca.Id);
        if (fatura is not null)
            return fatura;

        // Cobrança criada, mas a resposta não chegou: a referência externa identifica a fatura.
        if (cobranca.ReferenciaExterna?.StartsWith("fatura-", StringComparison.Ordinal) == true &&
            int.TryParse(cobranca.ReferenciaExterna["fatura-".Length..], out var faturaId))
        {
            fatura = await _repository.ObterFaturaAsync(faturaId);
            if (fatura is not null && fatura.AsaasCobrancaId is null)
                fatura.AsaasCobrancaId = cobranca.Id;
        }

        return fatura;
    }

    /// <summary>
    /// Marca como vencidas as faturas pendentes com vencimento passado e recalcula a situação
    /// financeira das instituições (todas, ou só a informada).
    /// </summary>
    private async Task<(int Vencidas, int Atualizadas)> AplicarReguaAsync(DateOnly hoje, int? somenteTenantId = null)
    {
        var faturas = await _repository.ListarFaturasAsync(somenteTenantId);

        var vencidas = 0;
        foreach (var fatura in faturas.Where(f => f.Situacao == FaturaAssinatura.SituacaoPendente && f.Vencimento < hoje))
        {
            fatura.Situacao = FaturaAssinatura.SituacaoVencida;
            fatura.UpdatedAt = DateTime.UtcNow;
            vencidas++;
        }

        IReadOnlyList<Tenant> instituicoes;
        if (somenteTenantId is int id)
        {
            var instituicaoUnica = await _repository.ObterInstituicaoAsync(id);
            instituicoes = instituicaoUnica is null ? [] : [instituicaoUnica];
        }
        else
        {
            instituicoes = await _repository.ListarInstituicoesAsync();
        }

        var atualizadas = 0;
        foreach (var instituicao in instituicoes)
        {
            var maisAntiga = faturas
                .Where(f => f.TenantId == instituicao.Id && f.EmAberto && f.Vencimento < hoje)
                .Select(f => (DateOnly?)f.Vencimento)
                .Min();

            var situacao = SituacaoFinanceira(maisAntiga, hoje);
            if (instituicao.SituacaoFinanceira == situacao)
                continue;

            _logger.LogInformation("Instituição {TenantId}: situação financeira {Anterior} -> {Nova}",
                instituicao.Id, instituicao.SituacaoFinanceira, situacao);
            instituicao.SituacaoFinanceira = situacao;
            atualizadas++;
        }

        return (vencidas, atualizadas);
    }

    private string SituacaoFinanceira(DateOnly? vencimentoMaisAntigo, DateOnly hoje)
    {
        if (vencimentoMaisAntigo is not DateOnly vencimento)
            return Tenant.SituacaoFinanceiraRegular;

        var dias = hoje.DayNumber - vencimento.DayNumber;
        if (dias >= _options.DiasAtrasoSomenteLeitura)
            return Tenant.SituacaoFinanceiraSomenteLeitura;

        return dias > 0 ? Tenant.SituacaoFinanceiraEmAtraso : Tenant.SituacaoFinanceiraRegular;
    }

    private static void MarcarComoPaga(FaturaAssinatura fatura, DateOnly data, decimal valor, string? forma)
    {
        fatura.Situacao = FaturaAssinatura.SituacaoPaga;
        fatura.PagaEm = data;
        fatura.ValorPago = valor;
        fatura.FormaPagamento = forma;
        fatura.UpdatedAt = DateTime.UtcNow;
    }

    private static string? SituacaoNota(string? statusAsaas) => statusAsaas switch
    {
        "SCHEDULED" or "SYNCHRONIZED" => FaturaAssinatura.NotaAgendada,
        "AUTHORIZED" or "CANCELLATION_DENIED" => FaturaAssinatura.NotaEmitida,
        "CANCELED" => FaturaAssinatura.NotaCancelada,
        "ERROR" => FaturaAssinatura.NotaErro,
        _ => null,
    };

    public static DateOnly ProximoVencimento(DateOnly hoje, int dia)
    {
        var candidato = new DateOnly(hoje.Year, hoje.Month, dia);
        return candidato >= hoje ? candidato : candidato.AddMonths(1);
    }

    private static DateOnly Competencia(DateOnly data) => new(data.Year, data.Month, 1);

    private static decimal ValorComDesconto(decimal valor, decimal descontoPercentual)
        => Math.Round(valor * (1 - descontoPercentual / 100m), 2, MidpointRounding.AwayFromZero);

    private static string ReferenciaFatura(FaturaAssinatura fatura) => $"fatura-{fatura.Id}";

    private static string DescricaoCobranca(FaturaAssinatura fatura)
    {
        var alunos = fatura.ValorPorAluno > 0 ? $" ({fatura.AlunosAtivos} alunos ativos)" : string.Empty;
        return Truncar($"Diário X - assinatura {fatura.Competencia:MM/yyyy} - {fatura.Assinatura.Tenant.Nome}{alunos}", 500);
    }

    private string DescricaoServico(FaturaAssinatura fatura)
        => Truncar(_options.NotaFiscal.DescricaoServico
            .Replace("{competencia}", fatura.Competencia.ToString("MM/yyyy", CultureInfo.InvariantCulture))
            .Replace("{alunos}", fatura.AlunosAtivos.ToString(CultureInfo.InvariantCulture)), 1000);

    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string Truncar(string valor, int max) => valor.Length <= max ? valor : valor[..max];

    private static FaturamentoResult<T> Invalid<T>(string message) => new(default, message, FaturamentoErro.Validation);

    private static PlanoResponse MapPlano(PlanoAssinatura p, int assinaturas)
        => new(p.Id, p.Nome, p.Descricao, p.ValorFixo, p.ValorPorAluno, p.ValorMinimo, p.Ativo, assinaturas);

    private static AssinaturaResponse MapAssinatura(Assinatura a)
        => new(a.Id, a.TenantId, a.Tenant.Nome, a.PlanoId, a.Plano.Nome, a.Situacao, a.DataInicio, a.TesteAte,
            a.DiaVencimento, a.DescontoPercentual, a.RazaoSocial, a.CpfCnpj, a.Email, a.Telefone, a.Cep, a.Endereco,
            a.Numero, a.Complemento, a.Bairro, a.AsaasClienteId is not null);

    private static FaturaResponse Map(FaturaAssinatura f, DateOnly hoje)
        => new(f.Id, f.TenantId, f.Assinatura.Tenant.Nome, f.Competencia, f.Vencimento, f.AlunosAtivos,
            f.ValorFixo, f.ValorPorAluno, f.ValorMinimo, f.ValorCalculado, f.DescontoPercentual, f.Valor, f.Situacao,
            f.EmAberto && f.Vencimento < hoje ? hoje.DayNumber - f.Vencimento.DayNumber : 0,
            f.AsaasCobrancaId, f.LinkPagamento, f.PagaEm, f.ValorPago, f.FormaPagamento,
            f.NotaFiscalSituacao, f.NotaFiscalNumero, f.NotaFiscalPdfUrl, f.NotaFiscalErro, f.Observacao, f.CreatedAt);
}
