using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Relatorios;
using DiarioX.Server.Application.Transferencias;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

/// <summary>
/// RF014: transferência do aluno e a declaração correspondente. Para fora da rede é uma saída definitiva;
/// entre escolas da rede o aluno passa para a escola de destino aguardando enturmação.
/// </summary>
public class TransferenciaAlunoService : ITransferenciaAlunoService
{
    private const string MensagemDataInvalida =
        "A data de transferência deve estar dentro do período do Ano Letivo vigente e não pode ser uma data futura.";

    private readonly IAlunoRepository _alunoRepository;
    private readonly IAlunoTurmaRepository _alunoTurmaRepository;
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDeclaracaoTransferenciaPdf _declaracaoPdf;
    private readonly IEscolaRepository _escolaRepository;

    public TransferenciaAlunoService(
        IAlunoRepository alunoRepository,
        IAlunoTurmaRepository alunoTurmaRepository,
        IAnoLetivoRepository anoLetivoRepository,
        ITransferenciaRepository transferenciaRepository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IDeclaracaoTransferenciaPdf declaracaoPdf,
        IEscolaRepository escolaRepository)
    {
        _escolaRepository = escolaRepository;
        _alunoRepository = alunoRepository;
        _alunoTurmaRepository = alunoTurmaRepository;
        _anoLetivoRepository = anoLetivoRepository;
        _transferenciaRepository = transferenciaRepository;
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _declaracaoPdf = declaracaoPdf;
    }

    public async Task<IReadOnlyList<TransferenciaListaItemResponse>> ListAsync()
    {
        return (await _transferenciaRepository.ListAsync()).Select(t => new TransferenciaListaItemResponse(
            t.Id,
            t.AlunoId,
            t.Aluno.Matricula,
            t.Aluno.Nome,
            t.EscolaOrigemId,
            t.EscolaOrigem.Nome,
            t.Turma?.ModalidadeEnsinoId,
            t.Turma?.ModalidadeEnsino.Nome,
            t.Turma?.EtapaEnsinoId,
            t.Turma?.EtapaEnsino.Nome,
            t.TurmaId,
            t.Turma?.NomeIdentificador,
            t.Turma?.NomeCompleto,
            t.Turma?.Turno,
            t.DataTransferencia)).ToList();
    }

    public async Task<IReadOnlyList<TransferenciaResponse>> GetByAlunoIdAsync(int alunoId)
    {
        return (await _transferenciaRepository.GetByAlunoIdAsync(alunoId)).Select(Map).ToList();
    }

    public async Task<TransferenciaResult> TransferirAsync(UsuarioAtual usuario, int alunoId, TransferenciaRequest request)
    {
        var aluno = alunoId > 0 ? await _alunoRepository.GetByIdAsync(alunoId) : null;
        if (aluno is null)
            return new(false, "Aluno não encontrado.", AlunoResultError.NotFound);

        // EX02
        if (aluno.Status == Aluno.StatusTransferido)
            return new(false, "Este aluno já possui o status de Transferido no sistema.", AlunoResultError.Conflict);

        if (aluno.Status != Aluno.StatusAtivo && aluno.Status != Aluno.StatusAtivoAguardandoEnturmacao)
            return Invalido("Somente alunos matriculados ou aguardando enturmação podem ser transferidos.");

        if (request.DataTransferencia == default)
            return Invalido("Informe a data do desligamento/transferência.");

        var tipo = (request.Tipo ?? string.Empty).Trim().ToUpperInvariant();
        if (!Transferencia.Tipos.ContainsKey(tipo))
            return Invalido("Selecione o tipo de transferência.");

        var motivo = string.IsNullOrWhiteSpace(request.Motivo) ? null : request.Motivo.Trim();
        if (motivo?.Length > Transferencia.MaxMotivo)
            return Invalido($"O motivo da saída deve ter no máximo {Transferencia.MaxMotivo} caracteres.");

        // Enturmado: vale o ano letivo da turma. Aguardando enturmação: o ano letivo em curso.
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var vinculo = await _alunoTurmaRepository.GetAtivaByAlunoIdAsync(alunoId);
        var anoLetivo = vinculo?.Turma.AnoLetivo ?? await _anoLetivoRepository.GetVigenteAsync(hoje);
        if (anoLetivo is null)
            return Invalido("Não há ano letivo vigente cadastrado para registrar a transferência.");

        // EX01
        var data = request.DataTransferencia;
        if (data > hoje || data < anoLetivo.DataInicio || data > anoLetivo.DataTermino)
            return Invalido(MensagemDataInvalida);

        if (vinculo is not null && data < vinculo.DataInicio)
            return Invalido($"A data de transferência não pode ser anterior ao início da enturmação atual ({vinculo.DataInicio:dd/MM/yyyy}).");

        var escolaOrigemId = vinculo?.Turma.EscolaId ?? aluno.EscolaId;
        int? escolaDestinoId = null;
        string escolaDestino;
        if (tipo == Transferencia.TipoEntreEscolasDaRede)
        {
            // A escola de destino pode estar fora do escopo de quem transfere: vale a rede inteira.
            var destino = request.EscolaDestinoId is int id
                ? (await _escolaRepository.GetAtivasDaInstituicaoAsync()).FirstOrDefault(e => e.Id == id)
                : null;
            if (destino is null)
                return Invalido("Selecione a escola da rede que vai receber o aluno.");
            if (destino.Id == escolaOrigemId)
                return Invalido("A escola de destino deve ser diferente da escola atual do aluno.");

            escolaDestinoId = destino.Id;
            escolaDestino = destino.Nome;
        }
        else
        {
            escolaDestino = (request.EscolaDestino ?? string.Empty).Trim();
            if (escolaDestino.Length == 0)
                return Invalido("Informe a escola de destino.");
            if (escolaDestino.Length > Transferencia.MaxEscolaDestino)
                return Invalido($"A escola de destino deve ter no máximo {Transferencia.MaxEscolaDestino} caracteres.");
        }

        var transferencia = new Transferencia
        {
            AlunoId = aluno.Id,
            EscolaOrigemId = escolaOrigemId,
            TurmaId = vinculo?.TurmaId,
            AnoLetivoId = anoLetivo.Id,
            DataTransferencia = data,
            Tipo = tipo,
            EscolaDestino = escolaDestino,
            EscolaDestinoId = escolaDestinoId,
            Motivo = motivo,
            RegistradoPorUsuarioId = usuario.UsuarioId,
        };

        try
        {
            await _transferenciaRepository.TransferirAsync(transferencia, vinculo?.Id);
        }
        catch (InvalidOperationException exception)
        {
            return new(false, exception.Message, AlunoResultError.Conflict);
        }

        var salva = await _transferenciaRepository.GetByIdAsync(transferencia.Id);
        var mensagem = escolaDestinoId is null
            ? "Transferência realizada com sucesso!"
            : $"Transferência realizada com sucesso! O aluno agora aguarda enturmação na escola {escolaDestino}.";
        return new(true, mensagem, Transferencia: salva is null ? null : Map(salva));
    }

    public async Task<IReadOnlyList<EscolaDestinoResponse>> ListarEscolasDestinoAsync()
    {
        return (await _escolaRepository.GetAtivasDaInstituicaoAsync())
            .Select(e => new EscolaDestinoResponse(e.Id, e.Nome))
            .ToList();
    }

    public async Task<DeclaracaoTransferenciaArquivo?> GerarDeclaracaoAsync(int transferenciaId)
    {
        var transferencia = await _transferenciaRepository.GetByIdAsync(transferenciaId);
        if (transferencia is null)
            return null;

        var instituicao = _tenantContext.TenantId is int tenantId
            ? (await _tenantRepository.GetByIdAsync(tenantId))?.Nome
            : null;

        var escola = transferencia.EscolaOrigem;
        var aluno = transferencia.Aluno;
        var dados = new DeclaracaoTransferenciaDados(
            instituicao,
            escola.Nome,
            Preenchido(escola.CodigoInep),
            Preenchido(escola.Municipio),
            Preenchido(escola.EnderecoCompleto),
            Preenchido(escola.Telefone),
            aluno.Nome,
            aluno.Matricula,
            aluno.DataNascimento,
            Preenchido(aluno.ResponsavelNome1),
            transferencia.Turma?.NomeCompleto,
            transferencia.AnoLetivo.AnoReferencia,
            transferencia.DataTransferencia,
            Transferencia.Tipos.GetValueOrDefault(transferencia.Tipo, transferencia.Tipo),
            transferencia.EscolaDestino,
            transferencia.Motivo,
            FormatacaoRelatorio.AgoraEmBrasilia());

        return new(_declaracaoPdf.Gerar(dados), $"declaracao-transferencia-{aluno.Matricula}.pdf");
    }

    private static string? Preenchido(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static TransferenciaResult Invalido(string message) => new(false, message, AlunoResultError.Validation);

    private static TransferenciaResponse Map(Transferencia t) => new(
        t.Id,
        t.AlunoId,
        t.Aluno.Nome,
        t.Aluno.Matricula,
        t.EscolaOrigemId,
        t.EscolaOrigem.Nome,
        t.TurmaId,
        t.Turma?.NomeCompleto,
        t.AnoLetivo.AnoReferencia,
        t.DataTransferencia,
        t.Tipo,
        Transferencia.Tipos.GetValueOrDefault(t.Tipo, t.Tipo),
        t.EscolaDestino,
        t.Motivo,
        t.CreatedAt,
        t.EscolaDestinoId);
}
