using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Calendario;
using DiarioX.Server.Application.DTOs.Calendario;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

/// <summary>RF005A: configuração e publicação do Calendário Letivo da rede ou de uma escola.</summary>
public class CalendarioLetivoService : ICalendarioLetivoService
{
    private readonly ICalendarioLetivoRepository _calendarioRepository;
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly IEscolaRepository _escolaRepository;
    private readonly ITenantContext _tenantContext;

    public CalendarioLetivoService(
        ICalendarioLetivoRepository calendarioRepository,
        IAnoLetivoRepository anoLetivoRepository,
        IEscolaRepository escolaRepository,
        ITenantContext tenantContext)
    {
        _calendarioRepository = calendarioRepository;
        _anoLetivoRepository = anoLetivoRepository;
        _escolaRepository = escolaRepository;
        _tenantContext = tenantContext;
    }

    private bool PodeEditarRede => _tenantContext.EscolaIds is null;

    public async Task<CalendarioOpcoesResponse> GetOpcoesAsync()
    {
        var anos = (await _anoLetivoRepository.GetAllAsync())
            .OrderByDescending(a => a.AnoReferencia)
            .Select(a => new CalendarioAnoOpcao(a.Id, a.AnoReferencia, a.DataInicio, a.DataTermino))
            .ToList();

        var escolas = (await _escolaRepository.GetAllAsync())
            .Where(e => e.Status == Escola.StatusAtivo)
            .OrderBy(e => e.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Select(e => new CalendarioEscolaOpcao(e.Id, e.Nome))
            .ToList();

        return new CalendarioOpcoesResponse(anos, escolas, PodeEditarRede);
    }

    public async Task<CalendarioCommandResult> GetAsync(int anoLetivoId, int? escolaId)
    {
        var (ano, escola, erro) = await CarregarAsync(anoLetivoId, escolaId);
        if (erro is not null)
            return erro;

        return new(true, string.Empty, await MontarRespostaAsync(ano!, escola));
    }

    public async Task<CalendarioCommandResult> SalvarEventoAsync(EventoCalendarioRequest request)
    {
        var (ano, escola, erro) = await CarregarParaEdicaoAsync(request.AnoLetivoId, request.EscolaId);
        if (erro is not null)
            return erro;

        var tipo = (request.Tipo ?? string.Empty).Trim().ToUpperInvariant();
        if (!EventoCalendario.Tipos.ContainsKey(tipo))
            return Invalid("Selecione o tipo do evento.");

        var descricao = request.Descricao?.Trim();
        if (string.IsNullOrEmpty(descricao))
            return Invalid("Informe a descrição (nome) do evento.");
        if (descricao.Length > EventoCalendario.MaxDescricao)
            return Invalid($"A descrição do evento deve ter no máximo {EventoCalendario.MaxDescricao} caracteres.");

        if (request.ComAula is not bool comAula)
            return Invalid("Informe se o dia é considerado letivo (com aula) ou sem aula.");

        var (de, ate, erroIntervalo) = ValidarIntervalo(ano!, request.DataInicio, request.DataFim);
        if (erroIntervalo is not null)
            return erroIntervalo;

        var eventos = new List<EventoCalendario>();
        for (var dia = de; dia <= ate; dia = dia.AddDays(1))
            eventos.Add(new EventoCalendario { Data = dia, Tipo = tipo, Descricao = descricao, ComAula = comAula });

        var calendario = await _calendarioRepository.GetOrCreateAsync(ano!.Id, escola?.Id);
        await _calendarioRepository.SubstituirEventosAsync(calendario.Id, de, ate, eventos);

        var mensagem = eventos.Count == 1 ? "Evento salvo com sucesso!" : $"Evento aplicado a {eventos.Count} dias com sucesso!";
        return new(true, mensagem, await MontarRespostaAsync(ano, escola));
    }

    public async Task<CalendarioCommandResult> RemoverEventosAsync(int anoLetivoId, int? escolaId, DateOnly de, DateOnly? ate)
    {
        var (ano, escola, erro) = await CarregarParaEdicaoAsync(anoLetivoId, escolaId);
        if (erro is not null)
            return erro;

        var (inicio, fim, erroIntervalo) = ValidarIntervalo(ano!, de, ate);
        if (erroIntervalo is not null)
            return erroIntervalo;

        var calendario = await _calendarioRepository.GetAsync(ano!.Id, escola?.Id);
        var removidos = calendario?.Eventos.Count(e => e.Data >= inicio && e.Data <= fim) ?? 0;
        if (removidos == 0)
            return Invalid("Não há eventos deste calendário nos dias selecionados.");

        await _calendarioRepository.SubstituirEventosAsync(calendario!.Id, inicio, fim, []);
        var mensagem = removidos == 1 ? "Evento removido com sucesso!" : $"{removidos} eventos removidos com sucesso!";
        return new(true, mensagem, await MontarRespostaAsync(ano, escola));
    }

    public async Task<CalendarioCommandResult> PublicarAsync(UsuarioAtual usuario, CalendarioPublicacaoRequest request)
    {
        var (ano, escola, erro) = await CarregarParaEdicaoAsync(request.AnoLetivoId, request.EscolaId);
        if (erro is not null)
            return erro;

        var calendario = await _calendarioRepository.GetOrCreateAsync(ano!.Id, escola?.Id);
        await _calendarioRepository.PublicarAsync(calendario.Id, usuario.UsuarioId);

        return new(true, $"Calendário Letivo do Ano {ano.AnoReferencia} configurado e publicado com sucesso!",
            await MontarRespostaAsync(ano, escola));
    }

    // ---------- Regras ----------

    private async Task<(AnoLetivo? Ano, Escola? Escola, CalendarioCommandResult? Erro)> CarregarAsync(int anoLetivoId, int? escolaId)
    {
        var ano = await _anoLetivoRepository.GetByIdAsync(anoLetivoId);
        if (ano is null)
            return (null, null, new(false, "Ano letivo não encontrado.", Error: CalendarioResultError.NotFound));

        if (escolaId is null)
            return (ano, null, null);

        // O repositório respeita o escopo de escolas do usuário: escola de fora do escopo não é encontrada.
        var escola = await _escolaRepository.GetByIdAsync(escolaId.Value);
        if (escola is null)
            return (null, null, new(false, "Escola não encontrada.", Error: CalendarioResultError.NotFound));

        return (ano, escola, null);
    }

    private async Task<(AnoLetivo? Ano, Escola? Escola, CalendarioCommandResult? Erro)> CarregarParaEdicaoAsync(int anoLetivoId, int? escolaId)
    {
        var carregado = await CarregarAsync(anoLetivoId, escolaId);
        if (carregado.Erro is null && escolaId is null && !PodeEditarRede)
        {
            return (null, null, new(false,
                "Seu perfil está vinculado a escolas específicas: altere o calendário da sua escola. " +
                "O calendário da rede é definido pela gestão da rede.",
                Error: CalendarioResultError.Forbidden));
        }

        return carregado;
    }

    /// <summary>EX02: o intervalo precisa estar dentro do ano letivo.</summary>
    private static (DateOnly De, DateOnly Ate, CalendarioCommandResult? Erro) ValidarIntervalo(AnoLetivo ano, DateOnly de, DateOnly? ate)
    {
        if (de == default)
            return (de, de, Invalid("Selecione o dia do evento."));

        var fim = ate ?? de;
        if (fim < de)
            return (de, fim, Invalid("A data final do intervalo deve ser igual ou posterior à data inicial."));

        if (de < ano.DataInicio || fim > ano.DataTermino)
            return (de, fim, Invalid("A data selecionada está fora do período do Ano Letivo configurado."));

        return (de, fim, null);
    }

    private async Task<CalendarioLetivoResponse> MontarRespostaAsync(AnoLetivo ano, Escola? escola)
    {
        var calendario = await _calendarioRepository.GetAsync(ano.Id, escola?.Id);

        // A escola herda os eventos do calendário da rede, desde que ele esteja publicado.
        var rede = escola is null ? null : await _calendarioRepository.GetAsync(ano.Id, null);
        var redeVigente = rede is { Publicado: true } ? rede : null;

        var eventos = CalendarioEfetivo.Mesclar(redeVigente, calendario);
        var diasLetivos = CalendarioEfetivo.ContarDiasLetivos(ano, eventos);

        return new CalendarioLetivoResponse(
            ano.Id,
            ano.AnoReferencia,
            ano.DataInicio,
            ano.DataTermino,
            ano.TipoPeriodo,
            ano.Periodos.OrderBy(p => p.Numero)
                .Select(p => new CalendarioPeriodoResponse(p.Id, p.Nome, p.Numero, p.DataInicio, p.DataTermino)).ToList(),
            escola?.Id,
            escola?.Nome,
            calendario?.Publicado ?? false,
            calendario?.PublicadoEm,
            rede?.Publicado ?? false,
            diasLetivos,
            CalendarioLetivo.MetaDiasLetivos,
            escola is not null || PodeEditarRede,
            eventos.Values
                .OrderBy(e => e.Data)
                .Select(e => new EventoCalendarioResponse(
                    e.Data, e.Tipo, EventoCalendario.Tipos.GetValueOrDefault(e.Tipo, e.Tipo), e.Descricao, e.ComAula,
                    Herdado: escola is not null && e.CalendarioLetivoId == rede?.Id))
                .ToList());
    }

    private static CalendarioCommandResult Invalid(string message)
        => new(false, message, Error: CalendarioResultError.Validation);
}
