using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Chamadas;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class ChamadaService : IChamadaService
{
    /// <summary>Frequência mínima exigida pela LDB (art. 24, VI).</summary>
    public const decimal FrequenciaMinima = 75m;

    private const int MaxConteudo = 2000;
    private const int MaxJustificativa = 255;

    private readonly IChamadaRepository _chamadaRepository;
    private readonly ITurmaRepository _turmaRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly IProfessorRepository _professorRepository;
    private readonly IProfessorAlocacaoRepository _alocacaoRepository;

    public ChamadaService(
        IChamadaRepository chamadaRepository,
        ITurmaRepository turmaRepository,
        IDisciplinaRepository disciplinaRepository,
        IAnoLetivoRepository anoLetivoRepository,
        IProfessorRepository professorRepository,
        IProfessorAlocacaoRepository alocacaoRepository)
    {
        _chamadaRepository = chamadaRepository;
        _turmaRepository = turmaRepository;
        _disciplinaRepository = disciplinaRepository;
        _anoLetivoRepository = anoLetivoRepository;
        _professorRepository = professorRepository;
        _alocacaoRepository = alocacaoRepository;
    }

    public async Task<IEnumerable<ChamadaTurmaResponse>> GetTurmasAsync(UsuarioAtual usuario)
    {
        var escopo = await GetEscopoAsync(usuario);
        var turmas = (await _turmaRepository.GetAllAsync()).Where(t => t.Status == Turma.StatusAtivo).ToList();
        var disciplinas = (await _disciplinaRepository.GetAllAsync()).Where(d => d.Ativa).ToList();
        var anos = (await _anoLetivoRepository.GetAllAsync()).ToDictionary(a => a.Id);

        return turmas
            .Select(turma =>
            {
                var permitidas = disciplinas
                    .Where(d => escopo is null ? FazParteDaGrade(d, turma) : escopo.Contains((turma.Id, d.Id)))
                    .OrderBy(d => d.Nome)
                    .Select(d => new ChamadaDisciplinaResponse(d.Id, d.Nome))
                    .ToList();

                var periodos = anos.TryGetValue(turma.AnoLetivoId, out var ano)
                    ? ano.Periodos.OrderBy(p => p.Numero)
                        .Select(p => new ChamadaPeriodoResponse(p.Id, p.Nome, p.DataInicio, p.DataTermino)).ToList()
                    : [];

                return new ChamadaTurmaResponse(
                    turma.Id, turma.NomeCompleto, turma.Escola.Nome, turma.AnoLetivo.AnoReferencia, turma.Turno,
                    turma.AnoLetivo.DataInicio, turma.AnoLetivo.DataTermino, permitidas, periodos);
            })
            .Where(t => t.Disciplinas.Count > 0)
            .OrderByDescending(t => t.AnoReferencia)
            .ThenBy(t => t.TurmaNome);
    }

    public async Task<ChamadaQueryResult<ChamadaResponse>> GetAsync(UsuarioAtual usuario, int turmaId, int disciplinaId, DateOnly data)
    {
        var (turma, erro) = await ValidarAcessoAsync(usuario, turmaId, disciplinaId);
        if (erro is not null)
            return new(default, erro.Message, erro.Error);

        var erroData = ValidarData(turma!, data);
        if (erroData is not null)
            return new(default, erroData, ChamadaResultError.Validation);

        var chamada = await _chamadaRepository.GetAsync(turmaId, disciplinaId, data);
        return new(await MontarRespostaAsync(turma!, disciplinaId, data, chamada));
    }

    public async Task<ChamadaQueryResult<IEnumerable<ChamadaResumoResponse>>> ListAsync(UsuarioAtual usuario, int turmaId, int disciplinaId)
    {
        var (_, erro) = await ValidarAcessoAsync(usuario, turmaId, disciplinaId);
        if (erro is not null)
            return new(default, erro.Message, erro.Error);

        var chamadas = await _chamadaRepository.ListAsync(turmaId, disciplinaId);
        var emails = await _chamadaRepository.GetEmailsUsuariosAsync(chamadas.Select(c => c.RegistradoPorUsuarioId));

        return new(chamadas.Select(c => new ChamadaResumoResponse(
            c.Id,
            c.Data,
            c.QuantidadeAulas,
            c.Registros.Count(r => r.Situacao == ChamadaAluno.SituacaoPresente),
            c.Registros.Count(r => r.Situacao == ChamadaAluno.SituacaoFalta),
            c.Registros.Count(r => r.Situacao == ChamadaAluno.SituacaoFaltaJustificada),
            c.Conteudo,
            emails.GetValueOrDefault(c.RegistradoPorUsuarioId),
            c.CreatedAt)).ToList());
    }

    public async Task<ChamadaQueryResult<FrequenciaResponse>> GetFrequenciaAsync(UsuarioAtual usuario, int turmaId, int disciplinaId, int? periodoId)
    {
        var (turma, erro) = await ValidarAcessoAsync(usuario, turmaId, disciplinaId);
        if (erro is not null)
            return new(default, erro.Message, erro.Error);

        var de = turma!.AnoLetivo.DataInicio;
        var ate = turma.AnoLetivo.DataTermino;
        if (periodoId is not null)
        {
            var ano = await _anoLetivoRepository.GetByIdAsync(turma.AnoLetivoId);
            var periodo = ano?.Periodos.FirstOrDefault(p => p.Id == periodoId);
            if (periodo is null)
                return new(default, "Período avaliativo não encontrado para o ano letivo da turma.", ChamadaResultError.NotFound);

            (de, ate) = (periodo.DataInicio, periodo.DataTermino);
        }

        var chamadas = await _chamadaRepository.ListAsync(turmaId, disciplinaId, de, ate);
        var enturmacoes = await _chamadaRepository.GetEnturmacoesAsync(turmaId, de, ate);

        // Alunos que passaram pela turma no período ou que têm registro nas chamadas dele.
        var alunos = enturmacoes.Select(e => e.Aluno)
            .Concat(chamadas.SelectMany(c => c.Registros).Select(r => r.Aluno))
            .GroupBy(a => a.Id)
            .Select(g => g.First());

        var linhas = alunos
            .Select(aluno =>
            {
                var registros = chamadas
                    .SelectMany(c => c.Registros.Where(r => r.AlunoId == aluno.Id).Select(r => (c.QuantidadeAulas, r.Situacao)))
                    .ToList();
                var aulas = registros.Sum(r => r.QuantidadeAulas);
                var faltas = registros.Where(r => r.Situacao == ChamadaAluno.SituacaoFalta).Sum(r => r.QuantidadeAulas);
                var justificadas = registros.Where(r => r.Situacao == ChamadaAluno.SituacaoFaltaJustificada).Sum(r => r.QuantidadeAulas);

                // Falta justificada continua sendo ausência para o cálculo da frequência.
                decimal? percentual = aulas == 0 ? null : Math.Round((aulas - faltas - justificadas) * 100m / aulas, 1);
                return new FrequenciaAlunoResponse(
                    aluno.Id, aluno.Matricula, aluno.Nome, aulas, faltas, justificadas,
                    percentual, percentual < FrequenciaMinima);
            })
            .OrderBy(l => l.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new(new FrequenciaResponse(de, ate, chamadas.Sum(c => c.QuantidadeAulas), FrequenciaMinima, linhas));
    }

    public async Task<ChamadaCommandResult> CreateAsync(UsuarioAtual usuario, ChamadaRequest request)
    {
        var (turma, erro) = await ValidarAcessoAsync(usuario, request.TurmaId, request.DisciplinaId);
        if (erro is not null)
            return erro;

        if (turma!.Status != Turma.StatusAtivo)
            return Invalid("Não é possível lançar chamada em uma turma inativa.");

        if (await _chamadaRepository.GetAsync(request.TurmaId, request.DisciplinaId, request.Data) is not null)
        {
            return new(false,
                "Já existe uma chamada registrada para esta turma, disciplina e data. Abra a chamada existente para alterá-la.",
                Error: ChamadaResultError.Conflict);
        }

        var (registros, erroDados) = await ValidarDadosAsync(turma, request, chamadaExistente: null);
        if (erroDados is not null)
            return erroDados;

        await _chamadaRepository.AddAsync(new Chamada
        {
            TurmaId = request.TurmaId,
            DisciplinaId = request.DisciplinaId,
            Data = request.Data,
            QuantidadeAulas = request.QuantidadeAulas,
            Conteudo = NormalizarConteudo(request.Conteudo),
            RegistradoPorUsuarioId = usuario.UsuarioId,
            Registros = registros!,
        });

        var salva = await _chamadaRepository.GetAsync(request.TurmaId, request.DisciplinaId, request.Data);
        return new(true, "Chamada registrada com sucesso!",
            await MontarRespostaAsync(turma, request.DisciplinaId, request.Data, salva));
    }

    public async Task<ChamadaCommandResult> UpdateAsync(UsuarioAtual usuario, int id, ChamadaRequest request)
    {
        var chamada = await _chamadaRepository.GetByIdAsync(id);
        if (chamada is null)
            return NotFound();

        // Turma, disciplina e data identificam a chamada e não mudam na edição.
        var (turma, erro) = await ValidarAcessoAsync(usuario, chamada.TurmaId, chamada.DisciplinaId);
        if (erro is not null)
            return erro;

        request.TurmaId = chamada.TurmaId;
        request.DisciplinaId = chamada.DisciplinaId;
        request.Data = chamada.Data;

        var (registros, erroDados) = await ValidarDadosAsync(turma!, request, chamada);
        if (erroDados is not null)
            return erroDados;

        chamada.QuantidadeAulas = request.QuantidadeAulas;
        chamada.Conteudo = NormalizarConteudo(request.Conteudo);
        chamada.AtualizadoPorUsuarioId = usuario.UsuarioId;
        chamada.UpdatedAt = DateTime.UtcNow;
        await _chamadaRepository.UpdateAsync(chamada, registros!);

        var salva = await _chamadaRepository.GetByIdAsync(id);
        return new(true, "Chamada atualizada com sucesso!",
            await MontarRespostaAsync(turma!, chamada.DisciplinaId, chamada.Data, salva));
    }

    public async Task<ChamadaCommandResult> DeleteAsync(UsuarioAtual usuario, int id)
    {
        var chamada = await _chamadaRepository.GetByIdAsync(id);
        if (chamada is null)
            return NotFound();

        var (_, erro) = await ValidarAcessoAsync(usuario, chamada.TurmaId, chamada.DisciplinaId);
        if (erro is not null)
            return erro;

        await _chamadaRepository.DeleteAsync(chamada);
        return new(true, "Chamada excluída com sucesso!");
    }

    // ---------- Regras ----------

    /// <summary>
    /// Nulo = acesso a todas as turmas (gestão e Administrador global). Para o usuário vinculado a um
    /// professor, só os pares turma/disciplina das alocações ativas dele.
    /// </summary>
    private async Task<HashSet<(int TurmaId, int DisciplinaId)>?> GetEscopoAsync(UsuarioAtual usuario)
    {
        if (usuario.IsGlobalAdmin)
            return null;

        var professor = await _professorRepository.GetByUsuarioIdAsync(usuario.UsuarioId);
        if (professor is null)
            return null;

        var alocacoes = await _alocacaoRepository.GetByProfessorIdAsync(professor.Id);
        return alocacoes.Select(a => (a.TurmaId, a.DisciplinaId)).ToHashSet();
    }

    private async Task<(Turma? Turma, ChamadaCommandResult? Erro)> ValidarAcessoAsync(UsuarioAtual usuario, int turmaId, int disciplinaId)
    {
        var turma = await _turmaRepository.GetByIdAsync(turmaId);
        if (turma is null)
            return (null, new(false, "Turma não encontrada.", Error: ChamadaResultError.NotFound));

        var disciplina = await _disciplinaRepository.GetByIdAsync(disciplinaId);
        if (disciplina is null)
            return (null, new(false, "Disciplina não encontrada.", Error: ChamadaResultError.NotFound));

        var escopo = await GetEscopoAsync(usuario);
        if (escopo is not null && !escopo.Contains((turmaId, disciplinaId)))
        {
            return (null, new(false,
                "Você só pode acessar a chamada das turmas e disciplinas em que está alocado.",
                Error: ChamadaResultError.Forbidden));
        }

        if (escopo is null && !FazParteDaGrade(disciplina, turma))
            return (null, Invalid("A disciplina não faz parte da grade desta turma."));

        return (turma, null);
    }

    // Mesma regra da alocação de professor: disciplina sem etapas vinculadas vale para qualquer etapa.
    private static bool FazParteDaGrade(Disciplina disciplina, Turma turma)
        => disciplina.EtapasEnsino.Count == 0 || disciplina.EtapasEnsino.Any(e => e.EtapaEnsinoId == turma.EtapaEnsinoId);

    private static string? ValidarData(Turma turma, DateOnly data)
    {
        if (data == default)
            return "Informe a data da chamada.";

        if (data > DateOnly.FromDateTime(DateTime.Today))
            return "A data da chamada não pode ser futura.";

        var ano = turma.AnoLetivo;
        if (data < ano.DataInicio || data > ano.DataTermino)
        {
            return $"A data da chamada deve estar dentro do ano letivo da turma " +
                   $"({ano.DataInicio:dd/MM/yyyy} a {ano.DataTermino:dd/MM/yyyy}).";
        }

        return null;
    }

    private async Task<(List<ChamadaAluno>? Registros, ChamadaCommandResult? Erro)> ValidarDadosAsync(
        Turma turma, ChamadaRequest request, Chamada? chamadaExistente)
    {
        var erroData = ValidarData(turma, request.Data);
        if (erroData is not null)
            return (null, Invalid(erroData));

        if (request.QuantidadeAulas < 1 || request.QuantidadeAulas > Chamada.MaxQuantidadeAulas)
            return (null, Invalid($"A quantidade de aulas deve estar entre 1 e {Chamada.MaxQuantidadeAulas}."));

        if ((request.Conteudo?.Trim().Length ?? 0) > MaxConteudo)
            return (null, Invalid($"O conteúdo da aula deve ter no máximo {MaxConteudo} caracteres."));

        var lista = await GetListaDeAlunosAsync(turma.Id, request.Data, chamadaExistente);
        if (lista.Count == 0)
            return (null, Invalid("Não há alunos enturmados nesta turma na data da chamada."));

        var informados = request.Alunos ?? new List<ChamadaAlunoRequest>();
        if (informados.GroupBy(a => a.AlunoId).Any(g => g.Count() > 1))
            return (null, Invalid("Um mesmo aluno foi informado mais de uma vez na chamada."));

        var foraDaTurma = informados.FirstOrDefault(a => !lista.ContainsKey(a.AlunoId));
        if (foraDaTurma is not null)
            return (null, Invalid("A chamada contém aluno que não está enturmado nesta turma na data informada."));

        var pendentes = lista.Values.Where(a => informados.All(i => i.AlunoId != a.Id)).Select(a => a.Nome).ToList();
        if (pendentes.Count > 0)
            return (null, Invalid($"Informe a situação de todos os alunos. Pendente(s): {string.Join(", ", pendentes)}."));

        var registros = new List<ChamadaAluno>();
        foreach (var item in informados)
        {
            var aluno = lista[item.AlunoId];
            var situacao = (item.Situacao ?? string.Empty).Trim().ToUpperInvariant();
            if (!ChamadaAluno.Situacoes.Contains(situacao))
                return (null, Invalid($"Situação inválida para {aluno.Nome}. Use Presente, Falta ou Falta justificada."));

            string? justificativa = null;
            if (situacao == ChamadaAluno.SituacaoFaltaJustificada)
            {
                justificativa = item.Justificativa?.Trim();
                if (string.IsNullOrEmpty(justificativa))
                    return (null, Invalid($"Informe a justificativa da falta de {aluno.Nome}."));
                if (justificativa.Length > MaxJustificativa)
                    return (null, Invalid($"A justificativa da falta de {aluno.Nome} deve ter no máximo {MaxJustificativa} caracteres."));
            }

            registros.Add(new ChamadaAluno { AlunoId = item.AlunoId, Situacao = situacao, Justificativa = justificativa });
        }

        return (registros, null);
    }

    /// <summary>
    /// Alunos da chamada na data: enturmados na turma naquele dia (sem os inativos) e, na edição,
    /// também quem já tem registro nela (ex.: remanejado depois, com data retroativa).
    /// </summary>
    private async Task<Dictionary<int, Aluno>> GetListaDeAlunosAsync(int turmaId, DateOnly data, Chamada? chamada)
    {
        var enturmados = (await _chamadaRepository.GetEnturmacoesAsync(turmaId, data, data))
            .Select(e => e.Aluno)
            .Where(a => a.Status != Aluno.StatusInativo);

        var registrados = chamada?.Registros.Select(r => r.Aluno) ?? [];

        return enturmados.Concat(registrados)
            .GroupBy(a => a.Id)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private async Task<ChamadaResponse> MontarRespostaAsync(Turma turma, int disciplinaId, DateOnly data, Chamada? chamada)
    {
        var lista = await GetListaDeAlunosAsync(turma.Id, data, chamada);
        var registros = chamada?.Registros.ToDictionary(r => r.AlunoId) ?? new Dictionary<int, ChamadaAluno>();

        var alunos = lista.Values
            .OrderBy(a => a.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Select(a =>
            {
                registros.TryGetValue(a.Id, out var registro);
                return new ChamadaAlunoResponse(a.Id, a.Matricula, a.Nome, registro?.Situacao, registro?.Justificativa);
            })
            .ToList();

        if (chamada is null)
            return new ChamadaResponse(null, turma.Id, disciplinaId, data, 1, null, null, null, null, null, alunos);

        var emails = await _chamadaRepository.GetEmailsUsuariosAsync(
            new[] { chamada.RegistradoPorUsuarioId, chamada.AtualizadoPorUsuarioId ?? 0 }.Where(id => id > 0));

        return new ChamadaResponse(
            chamada.Id, turma.Id, disciplinaId, data, chamada.QuantidadeAulas, chamada.Conteudo,
            emails.GetValueOrDefault(chamada.RegistradoPorUsuarioId), chamada.CreatedAt,
            chamada.AtualizadoPorUsuarioId is int atualizadoPor ? emails.GetValueOrDefault(atualizadoPor) : null,
            chamada.UpdatedAt,
            alunos);
    }

    private static string? NormalizarConteudo(string? conteudo)
        => string.IsNullOrWhiteSpace(conteudo) ? null : conteudo.Trim();

    private static ChamadaCommandResult Invalid(string message)
        => new(false, message, Error: ChamadaResultError.Validation);

    private static ChamadaCommandResult NotFound()
        => new(false, "Chamada não encontrada.", Error: ChamadaResultError.NotFound);
}
