using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.DTOs.Turmas;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class RemanejamentoAlunoService : IRemanejamentoAlunoService
{
    private const string AlunoTransferido =
        "Não é possível enturmar um aluno transferido. Para readmiti-lo, ative-o na tela de Alunos.";

    private readonly IAlunoRepository _alunoRepository;
    private readonly IAlunoTurmaRepository _alunoTurmaRepository;
    private readonly ITurmaRepository _turmaRepository;

    public RemanejamentoAlunoService(
        IAlunoRepository alunoRepository,
        IAlunoTurmaRepository alunoTurmaRepository,
        ITurmaRepository turmaRepository)
    {
        _alunoRepository = alunoRepository;
        _alunoTurmaRepository = alunoTurmaRepository;
        _turmaRepository = turmaRepository;
    }

    public async Task<EnturmacaoAtivaResponse?> GetEnturmacaoAtivaAsync(int alunoId)
    {
        var vinculo = await _alunoTurmaRepository.GetAtivaByAlunoIdAsync(alunoId);
        return vinculo is null ? null : new EnturmacaoAtivaResponse(
            vinculo.AlunoId,
            vinculo.Aluno.EscolaId,
            vinculo.Aluno.Escola.Nome,
            vinculo.TurmaId,
            vinculo.Turma.NomeCompleto,
            vinculo.Turma.AnoLetivoId,
            vinculo.Turma.AnoLetivo.AnoReferencia,
            vinculo.Turma.Turno,
            vinculo.DataInicio);
    }

    public async Task<RemanejamentoAlunoResult> EnturmarAsync(int alunoId, EnturmacaoAlunoRequest request)
    {
        if (alunoId <= 0 || request.TurmaId <= 0 || request.DataInicio == default)
            return Invalid("Por favor, informe a data de início e a turma.");

        var aluno = await _alunoRepository.GetByIdAsync(alunoId);
        if (aluno is null)
            return new(false, "Aluno não encontrado.", AlunoResultError.NotFound);

        if (aluno.Status is Aluno.StatusInativo or Aluno.StatusInativoObito)
            return Invalid("Não é possível enturmar um aluno inativo.");

        if (aluno.Status == Aluno.StatusTransferido)
            return Invalid(AlunoTransferido);

        if (await _alunoTurmaRepository.GetAtivaByAlunoIdAsync(alunoId) is not null)
            return Invalid("O aluno já possui enturmação ativa.");

        var turma = await _turmaRepository.GetByIdAsync(request.TurmaId);
        if (turma is null)
            return new(false, "Turma não encontrada.", AlunoResultError.NotFound);

        if (turma.Status != Turma.StatusAtivo || turma.EscolaId != aluno.EscolaId)
            return Invalid("A turma deve estar ativa e pertencer à mesma escola do aluno.");

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        if (request.DataInicio < turma.AnoLetivo.DataInicio || request.DataInicio > hoje)
            return Invalid("A data de início deve estar entre o início do ano letivo e a data atual.");

        if (!await _alunoTurmaRepository.HasVacancyAsync(turma.Id, request.DataInicio))
            return new(false, "A turma não possui vagas disponíveis.", AlunoResultError.Conflict);

        try
        {
            if (!await _alunoTurmaRepository.EnturmarAsync([alunoId], turma.Id, request.DataInicio))
                return new(false, "A turma não possui vagas disponíveis.", AlunoResultError.Conflict);
        }
        catch (InvalidOperationException exception)
        {
            return Invalid(exception.Message);
        }

        return new(true, $"Aluno enturmado com sucesso na turma {turma.NomeCompleto}!");
    }

    public async Task<VagasTurmaResponse?> GetVagasTurmaAsync(int turmaId, DateOnly data)
    {
        var turma = await _turmaRepository.GetByIdAsync(turmaId);
        if (turma is null)
            return null;

        var ocupadas = await _alunoTurmaRepository.GetOcupacaoMaximaAsync(turmaId, data);
        return new VagasTurmaResponse(turmaId, data, turma.VagasOfertadas, ocupadas, Math.Max(0, turma.VagasOfertadas - ocupadas));
    }

    /// <summary>
    /// Enturma vários alunos na mesma turma. É tudo ou nada: se algum aluno não puder ser enturmado
    /// ou faltarem vagas, nenhum vínculo é gravado e a resposta aponta os alunos com problema.
    /// </summary>
    public async Task<EnturmacaoLoteResult> EnturmarEmLoteAsync(EnturmacaoLoteRequest request)
    {
        if (request.TurmaId <= 0 || request.DataInicio == default)
            return LoteInvalido("Por favor, informe a data de início e a turma.");

        var alunoIds = request.AlunoIds.Distinct().ToList();
        if (alunoIds.Count == 0 || alunoIds.Any(id => id <= 0))
            return LoteInvalido("Selecione ao menos um aluno para enturmar.");

        var turma = await _turmaRepository.GetByIdAsync(request.TurmaId);
        if (turma is null)
            return new(false, "Turma não encontrada.", AlunoResultError.NotFound);

        if (turma.Status != Turma.StatusAtivo)
            return LoteInvalido("A turma deve estar ativa.");

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        if (request.DataInicio < turma.AnoLetivo.DataInicio || request.DataInicio > hoje)
            return LoteInvalido("A data de início deve estar entre o início do ano letivo e a data atual.");

        var alunos = (await _alunoRepository.GetByIdsAsync(alunoIds)).ToDictionary(aluno => aluno.Id);
        var jaEnturmados = (await _alunoTurmaRepository.GetAlunoIdsComEnturmacaoAtivaAsync(alunoIds)).ToHashSet();

        var falhas = alunoIds
            .Select(id => MotivoImpedimento(alunos.GetValueOrDefault(id), turma, jaEnturmados) is { } motivo
                ? new EnturmacaoLoteFalha(id, motivo)
                : null)
            .OfType<EnturmacaoLoteFalha>()
            .ToList();

        if (falhas.Count > 0)
        {
            var impedidos = falhas.Count == 1 ? "1 aluno não pode ser enturmado" : $"{falhas.Count} alunos não podem ser enturmados";
            return new(false, $"{impedidos} nesta turma. Nenhuma enturmação foi realizada.",
                AlunoResultError.Validation, falhas);
        }

        var disponiveis = turma.VagasOfertadas - await _alunoTurmaRepository.GetOcupacaoMaximaAsync(turma.Id, request.DataInicio);
        if (alunoIds.Count > disponiveis)
            return new(false, VagasInsuficientes(disponiveis, alunoIds.Count), AlunoResultError.Conflict);

        try
        {
            // A checagem é refeita com a turma travada: outra enturmação pode ter ocupado as vagas nesse meio-tempo.
            if (!await _alunoTurmaRepository.EnturmarAsync(alunoIds, turma.Id, request.DataInicio))
                return new(false, "As vagas da turma foram ocupadas por outra enturmação. Atualize a tela e tente novamente.",
                    AlunoResultError.Conflict);
        }
        catch (InvalidOperationException exception)
        {
            return LoteInvalido(exception.Message);
        }

        return new(true, alunoIds.Count == 1
            ? $"1 aluno enturmado com sucesso na turma {turma.NomeCompleto}!"
            : $"{alunoIds.Count} alunos enturmados com sucesso na turma {turma.NomeCompleto}!");
    }

    private static string? MotivoImpedimento(Aluno? aluno, Turma turma, HashSet<int> jaEnturmados)
    {
        if (aluno is null)
            return "Aluno não encontrado.";
        if (aluno.Status is Aluno.StatusInativo or Aluno.StatusInativoObito)
            return "Aluno inativo.";
        if (aluno.Status == Aluno.StatusTransferido)
            return "Aluno transferido.";
        if (jaEnturmados.Contains(aluno.Id))
            return "Aluno já possui enturmação ativa.";
        if (aluno.EscolaId != turma.EscolaId)
            return "Aluno pertence a outra escola.";
        return null;
    }

    private static string VagasInsuficientes(int disponiveis, int selecionados)
    {
        if (disponiveis <= 0)
            return "A turma não possui vagas disponíveis.";

        var vagas = disponiveis == 1 ? "1 vaga disponível" : $"{disponiveis} vagas disponíveis";
        return $"A turma possui {vagas}, mas {selecionados} alunos foram selecionados.";
    }

    private static EnturmacaoLoteResult LoteInvalido(string message)
        => new(false, message, AlunoResultError.Validation);

    public async Task<RemanejamentoAlunoResult> RemanejarAsync(int alunoId, RemanejamentoAlunoRequest request)
    {
        if (alunoId <= 0 || request.TurmaDestinoId <= 0 || request.DataMovimentacao == default)
            return Invalid("Por favor, informe a data da movimentação e a nova turma.");

        var vinculoOrigem = await _alunoTurmaRepository.GetAtivaByAlunoIdAsync(alunoId);
        if (vinculoOrigem is null)
            return new(false, "O aluno não possui enturmação ativa.", AlunoResultError.NotFound);

        if (vinculoOrigem.Aluno.Status == Aluno.StatusInativo)
            return Invalid("Não é possível remanejar um aluno inativo.");

        var resultado = await RemanejarAsync(vinculoOrigem.Turma, [alunoId], new Dictionary<int, AlunoTurma> { [alunoId] = vinculoOrigem },
            request.TurmaDestinoId, request.DataMovimentacao, request.Motivo);
        return new(resultado.Success, resultado.Message, resultado.Error);
    }

    /// <summary>
    /// Remaneja vários alunos da mesma turma de origem para a mesma turma de destino. É tudo ou nada:
    /// se algum aluno não puder ser remanejado ou faltarem vagas, nada é gravado.
    /// </summary>
    public async Task<RemanejamentoLoteResult> RemanejarEmLoteAsync(RemanejamentoLoteRequest request)
    {
        var alunoIds = request.AlunoIds.Distinct().ToList();
        if (alunoIds.Count == 0 || alunoIds.Any(id => id <= 0))
            return LoteRemanejamentoInvalido("Selecione ao menos um aluno para remanejar.");

        if (request.TurmaOrigemId <= 0 || request.TurmaDestinoId <= 0 || request.DataMovimentacao == default)
            return LoteRemanejamentoInvalido("Por favor, informe a data da movimentação e a nova turma.");

        var origem = await _turmaRepository.GetByIdAsync(request.TurmaOrigemId);
        if (origem is null)
            return new(false, "Turma de origem não encontrada.", AlunoResultError.NotFound);

        var vinculos = (await _alunoTurmaRepository.GetAtivasByTurmaIdAsync(origem.Id)).ToDictionary(v => v.AlunoId);
        return await RemanejarAsync(origem, alunoIds, vinculos, request.TurmaDestinoId, request.DataMovimentacao, request.Motivo);
    }

    /// <summary>
    /// Turmas que podem receber alunos da turma de origem: ativas, da mesma escola, do mesmo ano letivo e da
    /// mesma etapa, com vaga a partir da data. Nulo se a turma de origem não existir.
    /// </summary>
    public async Task<IReadOnlyList<TurmaDestinoResponse>?> GetDestinosRemanejamentoAsync(int turmaOrigemId, DateOnly data)
    {
        var origem = await _turmaRepository.GetByIdAsync(turmaOrigemId);
        if (origem is null)
            return null;

        var destinos = new List<TurmaDestinoResponse>();
        foreach (var turma in (await _turmaRepository.GetAllAsync()).Where(t => PodeReceber(origem, t)))
        {
            var disponiveis = turma.VagasOfertadas - await _alunoTurmaRepository.GetOcupacaoMaximaAsync(turma.Id, data);
            if (disponiveis > 0)
                destinos.Add(new TurmaDestinoResponse(turma.Id, turma.NomeCompleto, turma.Turno, turma.VagasOfertadas, disponiveis));
        }

        return destinos.OrderBy(d => d.NomeCompleto, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private async Task<RemanejamentoLoteResult> RemanejarAsync(Turma origem, IReadOnlyList<int> alunoIds,
        IReadOnlyDictionary<int, AlunoTurma> vinculos, int turmaDestinoId, DateOnly data, string? motivoInformado)
    {
        var motivo = string.IsNullOrWhiteSpace(motivoInformado) ? null : motivoInformado.Trim();
        if (motivo?.Length > AlunoTurma.MaxObservacaoDesenturmacao)
            return LoteRemanejamentoInvalido($"O motivo deve ter no máximo {AlunoTurma.MaxObservacaoDesenturmacao} caracteres.");

        var destino = await _turmaRepository.GetByIdAsync(turmaDestinoId);
        if (destino is null)
            return new(false, "Turma de destino não encontrada.", AlunoResultError.NotFound);

        if (destino.Id == origem.Id)
            return LoteRemanejamentoInvalido("A turma de destino deve ser diferente da turma atual do aluno.");

        if (!PodeReceber(origem, destino))
            return LoteRemanejamentoInvalido("A turma de destino deve ser uma turma ativa da mesma escola, do mesmo ano letivo e da mesma etapa da turma atual.");

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        if (data < origem.AnoLetivo.DataInicio || data > hoje)
            return LoteRemanejamentoInvalido("A data da movimentação deve estar entre o início do ano letivo e a data atual.");

        var impedimentos = alunoIds
            .Select(id => (Id: id, Impedimento: ImpedimentoDoRemanejamento(vinculos.GetValueOrDefault(id), data)))
            .Where(x => x.Impedimento is not null)
            .ToList();

        if (impedimentos.Count > 0)
        {
            var falhas = impedimentos.Select(x => new RemanejamentoFalha(x.Id, x.Impedimento!.Value.Curto)).ToList();
            var message = alunoIds.Count == 1
                ? impedimentos[0].Impedimento!.Value.Individual
                : $"{(falhas.Count == 1 ? "1 aluno não pode ser remanejado" : $"{falhas.Count} alunos não podem ser remanejados")}. Nenhum remanejamento foi realizado.";
            return new(false, message, AlunoResultError.Validation, falhas);
        }

        var disponiveis = destino.VagasOfertadas - await _alunoTurmaRepository.GetOcupacaoMaximaAsync(destino.Id, data);
        if (alunoIds.Count > disponiveis)
        {
            var message = alunoIds.Count == 1 || disponiveis <= 0
                ? "A turma de destino não possui vagas disponíveis para remanejamento."
                : $"A turma de destino possui {(disponiveis == 1 ? "1 vaga disponível" : $"{disponiveis} vagas disponíveis")}, mas {alunoIds.Count} alunos foram selecionados.";
            return new(false, message, AlunoResultError.Conflict);
        }

        try
        {
            // A checagem é refeita com as turmas travadas: outra movimentação pode ter ocupado as vagas nesse meio-tempo.
            if (!await _alunoTurmaRepository.RemanejarAsync(origem.Id, alunoIds, destino.Id, data, motivo))
                return new(false, "A turma de destino não possui vagas disponíveis para remanejamento.", AlunoResultError.Conflict);
        }
        catch (InvalidOperationException exception)
        {
            return new(false, exception.Message, AlunoResultError.Conflict);
        }

        return new(true, alunoIds.Count == 1
            ? $"Aluno remanejado com sucesso para a turma {destino.NomeCompleto}!"
            : $"{alunoIds.Count} alunos remanejados com sucesso para a turma {destino.NomeCompleto}!");
    }

    private static bool PodeReceber(Turma origem, Turma destino)
        => destino.Id != origem.Id &&
           destino.Status == Turma.StatusAtivo &&
           destino.EscolaId == origem.EscolaId &&
           destino.AnoLetivoId == origem.AnoLetivoId &&
           destino.EtapaEnsinoId == origem.EtapaEnsinoId;

    /// <summary>Motivo para a lista (curto) e para a mensagem de um aluno só (individual); nulo se pode ser remanejado.</summary>
    private static (string Curto, string Individual)? ImpedimentoDoRemanejamento(AlunoTurma? vinculo, DateOnly data)
    {
        if (vinculo is null)
            return ("Aluno não está enturmado nesta turma.", "O aluno não está enturmado na turma de origem.");
        if (vinculo.Aluno.Status == Aluno.StatusInativo)
            return ("Aluno inativo.", "Não é possível remanejar um aluno inativo.");
        if (data <= vinculo.DataInicio)
            return ($"Enturmado(a) em {vinculo.DataInicio:dd/MM/yyyy}; a movimentação deve ser posterior a essa data.",
                "A data da movimentação deve ser posterior ao início da enturmação atual.");
        return null;
    }

    private static RemanejamentoLoteResult LoteRemanejamentoInvalido(string message)
        => new(false, message, AlunoResultError.Validation);

    public async Task<IReadOnlyList<AlunoEnturmadoResponse>?> GetAlunosEnturmadosAsync(int turmaId)
    {
        if (await _turmaRepository.GetByIdAsync(turmaId) is null)
            return null;

        return (await _alunoTurmaRepository.GetAtivasByTurmaIdAsync(turmaId))
            .Select(v => new AlunoEnturmadoResponse(v.AlunoId, v.Aluno.Matricula, v.Aluno.Nome, v.Aluno.Status, v.DataInicio))
            .ToList();
    }

    public async Task<IReadOnlyList<EnturmacaoAtivaItemResponse>> GetEnturmacoesAtivasAsync()
    {
        return (await _alunoTurmaRepository.GetAtivasAsync())
            .Select(v => new EnturmacaoAtivaItemResponse(
                v.AlunoId,
                v.Aluno.Matricula,
                v.Aluno.Nome,
                v.Aluno.Status,
                v.DataInicio,
                v.TurmaId,
                v.Turma.NomeIdentificador,
                v.Turma.NomeCompleto,
                v.Turma.EscolaId,
                v.Turma.Escola.Nome,
                v.Turma.ModalidadeEnsinoId,
                v.Turma.ModalidadeEnsino.Nome,
                v.Turma.EtapaEnsinoId,
                v.Turma.EtapaEnsino.Nome,
                v.Turma.Turno))
            .ToList();
    }

    /// <summary>
    /// RF013: retira alunos da turma a partir de hoje, liberando as vagas, e muda o status de cada um
    /// conforme o motivo. É tudo ou nada: se algum aluno não estiver enturmado na turma, nada é gravado.
    /// Os lançamentos do diário (chamadas) não são tocados.
    /// </summary>
    public async Task<DesenturmacaoResult> DesenturmarAsync(DesenturmacaoRequest request)
    {
        var alunoIds = request.AlunoIds.Distinct().ToList();
        if (alunoIds.Count == 0 || alunoIds.Any(id => id <= 0))
            return DesenturmacaoInvalida("Selecione ao menos um aluno para realizar a desenturmação.");

        var motivo = (request.Motivo ?? string.Empty).Trim().ToUpperInvariant();
        if (motivo.Length == 0)
            return DesenturmacaoInvalida("Por favor, selecione o motivo da desenturmação para continuar.");

        if (!AlunoTurma.MotivosDesenturmacao.Contains(motivo))
            return DesenturmacaoInvalida("Motivo da desenturmação inválido.");

        var observacao = string.IsNullOrWhiteSpace(request.Observacao) ? null : request.Observacao.Trim();
        if (motivo == AlunoTurma.MotivoOutros && observacao is null)
            return DesenturmacaoInvalida("Informe a observação/justificativa quando o motivo for \"Outros\".");

        if (observacao?.Length > AlunoTurma.MaxObservacaoDesenturmacao)
            return DesenturmacaoInvalida($"A observação deve ter no máximo {AlunoTurma.MaxObservacaoDesenturmacao} caracteres.");

        var turma = await _turmaRepository.GetByIdAsync(request.TurmaId);
        if (turma is null)
            return new(false, "Turma não encontrada.", AlunoResultError.NotFound);

        var enturmados = (await _alunoTurmaRepository.GetAtivasByTurmaIdAsync(turma.Id)).ToDictionary(v => v.AlunoId, v => v.Aluno);
        var falhas = alunoIds
            .Where(id => !enturmados.ContainsKey(id))
            .Select(id => new DesenturmacaoFalha(id, "Aluno não está enturmado nesta turma."))
            .ToList();

        if (falhas.Count > 0)
        {
            var impedidos = falhas.Count == 1 ? "1 aluno não está enturmado" : $"{falhas.Count} alunos não estão enturmados";
            return new(false, $"{impedidos} nesta turma. Nenhuma desenturmação foi realizada.",
                AlunoResultError.Validation, falhas);
        }

        var statusPorAluno = alunoIds.ToDictionary(id => id, id => StatusAposDesenturmacao(enturmados[id], motivo));

        try
        {
            await _alunoTurmaRepository.DesenturmarAsync(turma.Id, statusPorAluno, DateOnly.FromDateTime(DateTime.Today),
                motivo, observacao);
        }
        catch (InvalidOperationException exception)
        {
            return new(false, exception.Message, AlunoResultError.Conflict);
        }

        if (alunoIds.Count > 1 && alunoIds.Count == enturmados.Count)
            return new(true, "Todos os alunos da turma foram desenturmados com sucesso!");

        return new(true, alunoIds.Count == 1
            ? "Aluno desenturmado com sucesso!"
            : $"{alunoIds.Count} alunos desenturmados com sucesso!");
    }

    /// <summary>RN01: o motivo define o status do aluno ao sair da turma.</summary>
    private static string StatusAposDesenturmacao(Aluno aluno, string motivo) => motivo switch
    {
        AlunoTurma.MotivoFalecimento => Aluno.StatusInativoObito,
        AlunoTurma.MotivoNaoCompareceu => Aluno.StatusNaoCompareceu,
        // Reestruturação, erro de matrícula e outros: volta a aguardar enturmação. Aluno inativado
        // continua inativo; a desenturmação não o reativa.
        _ => aluno.Status == Aluno.StatusInativo ? Aluno.StatusInativo : Aluno.StatusAtivoAguardandoEnturmacao,
    };

    private static DesenturmacaoResult DesenturmacaoInvalida(string message)
        => new(false, message, AlunoResultError.Validation);

    private static RemanejamentoAlunoResult Invalid(string message)
        => new(false, message, AlunoResultError.Validation);
}