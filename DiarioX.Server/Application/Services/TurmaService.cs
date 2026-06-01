using DiarioX.Server.Application.DTOs.Turmas;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class TurmaService : ITurmaService
{
    private static readonly Dictionary<string, string> TurnosValidos = new()
    {
        [Turma.TurnoManha] = "Manhã",
        [Turma.TurnoTarde] = "Tarde",
        [Turma.TurnoNoite] = "Noite",
        [Turma.TurnoIntegral] = "Integral",
    };

    private readonly ITurmaRepository _turmaRepository;
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly IEscolaRepository _escolaRepository;
    private readonly IModalidadeEnsinoRepository _modalidadeRepository;
    private readonly IEtapaEnsinoRepository _etapaRepository;

    public TurmaService(
        ITurmaRepository turmaRepository,
        IAnoLetivoRepository anoLetivoRepository,
        IEscolaRepository escolaRepository,
        IModalidadeEnsinoRepository modalidadeRepository,
        IEtapaEnsinoRepository etapaRepository)
    {
        _turmaRepository = turmaRepository;
        _anoLetivoRepository = anoLetivoRepository;
        _escolaRepository = escolaRepository;
        _modalidadeRepository = modalidadeRepository;
        _etapaRepository = etapaRepository;
    }

    public async Task<IEnumerable<TurmaResponse>> GetAllAsync()
    {
        var turmas = await _turmaRepository.GetAllAsync();
        return turmas.Select(MapToResponse);
    }

    public async Task<TurmaResponse?> GetByIdAsync(int id)
    {
        var turma = await _turmaRepository.GetByIdAsync(id);
        return turma is null ? null : MapToResponse(turma);
    }

    public async Task<TurmaCommandResult> CreateAsync(TurmaRequest request)
    {
        var normalized = NormalizeRequest(request);
        var validation = await ValidateRequestAsync(normalized);
        if (!validation.Success)
            return validation;

        var anoLetivo = await _anoLetivoRepository.GetByIdAsync(normalized.AnoLetivoId);
        if (anoLetivo is null)
            return NotFound("Ano letivo não encontrado.");

        var escola = await _escolaRepository.GetByIdAsync(normalized.EscolaId);
        if (escola is null)
            return NotFound("Escola não encontrada.");

        var modalidade = await _modalidadeRepository.GetByIdAsync(normalized.ModalidadeEnsinoId);
        if (modalidade is null)
            return NotFound("Modalidade de ensino não encontrada.");

        var etapa = await _etapaRepository.GetByIdAsync(normalized.EtapaEnsinoId);
        if (etapa is null)
            return NotFound("Ano de ensino / etapa não encontrado.");

        if (etapa.ModalidadeEnsinoId != modalidade.Id)
            return Invalid("A etapa de ensino selecionada não pertence à modalidade informada.");

        var exists = await _turmaRepository.ExistsByCombinacaoAsync(
            normalized.AnoLetivoId,
            normalized.EscolaId,
            normalized.EtapaEnsinoId,
            normalized.NomeIdentificador,
            normalized.Turno);

        if (exists)
        {
            return new TurmaCommandResult(
                false,
                "Já existe uma turma cadastrada com essas mesmas características para esta escola.",
                Error: TurmaResultError.Conflict);
        }

        var turma = new Turma
        {
            AnoLetivoId = normalized.AnoLetivoId,
            EscolaId = normalized.EscolaId,
            ModalidadeEnsinoId = normalized.ModalidadeEnsinoId,
            EtapaEnsinoId = normalized.EtapaEnsinoId,
            NomeIdentificador = normalized.NomeIdentificador,
            NomeCompleto = BuildNomeCompleto(etapa.Nome, normalized.NomeIdentificador, modalidade.Nome, normalized.Turno),
            Turno = normalized.Turno,
            VagasOfertadas = normalized.VagasOfertadas,
            Status = Turma.StatusAtivo,
        };

        var created = await _turmaRepository.AddAsync(turma);
        var createdWithNav = await _turmaRepository.GetByIdAsync(created.Id);

        return new TurmaCommandResult(true, "Turma cadastrada com sucesso!", createdWithNav is null ? null : MapToResponse(createdWithNav));
    }

    private async Task<TurmaCommandResult> ValidateRequestAsync(TurmaRequest request)
    {
        if (request.AnoLetivoId <= 0 ||
            request.EscolaId <= 0 ||
            request.ModalidadeEnsinoId <= 0 ||
            request.EtapaEnsinoId <= 0 ||
            string.IsNullOrWhiteSpace(request.NomeIdentificador) ||
            string.IsNullOrWhiteSpace(request.Turno))
        {
            return Invalid("Por favor, preencha todos os campos obrigatórios.");
        }

        if (!TurnosValidos.ContainsKey(request.Turno))
            return Invalid("Turno inválido. Opções válidas: Manhã, Tarde, Noite e Integral.");

        if (request.VagasOfertadas <= 0)
            return Invalid("Vagas ofertadas deve ser maior que zero.");

        return new TurmaCommandResult(true, string.Empty);
    }

    private static TurmaRequest NormalizeRequest(TurmaRequest request)
    {
        return new TurmaRequest
        {
            AnoLetivoId = request.AnoLetivoId,
            EscolaId = request.EscolaId,
            ModalidadeEnsinoId = request.ModalidadeEnsinoId,
            EtapaEnsinoId = request.EtapaEnsinoId,
            NomeIdentificador = request.NomeIdentificador.Trim(),
            Turno = (request.Turno ?? string.Empty).Trim().ToUpperInvariant(),
            VagasOfertadas = request.VagasOfertadas,
        };
    }

    private static string BuildNomeCompleto(string etapaNome, string nomeIdentificador, string modalidadeNome, string turno)
    {
        var identificador = nomeIdentificador;
        if (identificador.StartsWith("Turma ", StringComparison.OrdinalIgnoreCase))
            identificador = identificador[6..].Trim();

        var etapaComIdentificador = $"{etapaNome} {identificador}".Trim();
        return $"{etapaComIdentificador} - {modalidadeNome} - {TurnosValidos[turno]}";
    }

    private static TurmaCommandResult Invalid(string message)
        => new(false, message, Error: TurmaResultError.Validation);

    private static TurmaCommandResult NotFound(string message)
        => new(false, message, Error: TurmaResultError.NotFound);

    private static TurmaResponse MapToResponse(Turma turma)
        => new(
            turma.Id,
            turma.AnoLetivoId,
            turma.AnoLetivo.AnoReferencia,
            turma.EscolaId,
            turma.Escola.Nome,
            turma.ModalidadeEnsinoId,
            turma.ModalidadeEnsino.Nome,
            turma.EtapaEnsinoId,
            turma.EtapaEnsino.Nome,
            turma.NomeIdentificador,
            turma.NomeCompleto,
            turma.Turno,
            TurnosValidos[turma.Turno],
            turma.VagasOfertadas,
            turma.Status
        );
}