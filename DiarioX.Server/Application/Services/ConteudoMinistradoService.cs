using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Calendario;
using DiarioX.Server.Application.DTOs.Conteudos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class ConteudoMinistradoService : IConteudoMinistradoService
{
    private const string AlertaSemConteudo = "Frequência lançada sem registro de conteúdo ministrado.";
    private const string AlertaSemFrequencia = "Conteúdo ministrado registrado sem frequência lançada.";

    private readonly IConteudoMinistradoRepository _conteudoRepository;
    private readonly IHabilidadeBnccRepository _habilidadeRepository;
    private readonly IChamadaRepository _chamadaRepository;
    private readonly ITurmaRepository _turmaRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly IProfessorRepository _professorRepository;
    private readonly IProfessorAlocacaoRepository _alocacaoRepository;
    private readonly ICalendarioLetivoRepository _calendarioRepository;

    public ConteudoMinistradoService(
        IConteudoMinistradoRepository conteudoRepository,
        IHabilidadeBnccRepository habilidadeRepository,
        IChamadaRepository chamadaRepository,
        ITurmaRepository turmaRepository,
        IDisciplinaRepository disciplinaRepository,
        IAnoLetivoRepository anoLetivoRepository,
        IProfessorRepository professorRepository,
        IProfessorAlocacaoRepository alocacaoRepository,
        ICalendarioLetivoRepository calendarioRepository)
    {
        _conteudoRepository = conteudoRepository;
        _habilidadeRepository = habilidadeRepository;
        _chamadaRepository = chamadaRepository;
        _turmaRepository = turmaRepository;
        _disciplinaRepository = disciplinaRepository;
        _anoLetivoRepository = anoLetivoRepository;
        _professorRepository = professorRepository;
        _alocacaoRepository = alocacaoRepository;
        _calendarioRepository = calendarioRepository;
    }

    public async Task<IEnumerable<ConteudoTurmaResponse>> GetTurmasAsync(UsuarioAtual usuario)
    {
        var escopo = await GetEscopoAsync(usuario);
        var turmas = (await _turmaRepository.GetAllAsync()).Where(t => t.Status == Turma.StatusAtivo).ToList();
        var disciplinas = (await _disciplinaRepository.GetAllAsync()).Where(d => d.Ativa).ToList();
        var anos = (await _anoLetivoRepository.GetAllAsync()).ToDictionary(a => a.Id);

        return turmas
            .Select(turma =>
            {
                var permitidas = disciplinas
                    .Where(d => PodeAcessar(escopo, turma, d))
                    .OrderBy(d => d.Nome)
                    .Select(d => new ConteudoDisciplinaResponse(d.Id, d.Nome))
                    .ToList();

                var periodos = anos.TryGetValue(turma.AnoLetivoId, out var ano)
                    ? ano.Periodos.OrderBy(p => p.Numero)
                        .Select(p => new ConteudoPeriodoResponse(p.Id, p.Nome, p.DataInicio, p.DataTermino)).ToList()
                    : [];

                return new ConteudoTurmaResponse(
                    turma.Id, turma.NomeCompleto, turma.Escola.Nome, turma.AnoLetivo.AnoReferencia,
                    turma.AnoLetivo.DataInicio, turma.AnoLetivo.DataTermino, permitidas, periodos,
                    turma.EtapaEnsino.TipoFrequencia);
            })
            .Where(t => t.Disciplinas.Count > 0)
            .OrderByDescending(t => t.AnoReferencia)
            .ThenBy(t => t.TurmaNome);
    }

    public async Task<ConteudoQueryResult<ConteudoMinistradoResponse>> GetAsync(
        UsuarioAtual usuario, int turmaId, int? disciplinaId, DateOnly data)
    {
        var (turma, disciplina, erro) = await ValidarAcessoAsync(usuario, turmaId, disciplinaId);
        if (erro is not null)
            return new(default, erro.Message, erro.Error);

        var erroData = ValidarData(turma!, data);
        if (erroData is not null)
            return new(default, erroData, ConteudoResultError.Validation);

        var conteudo = await _conteudoRepository.GetAsync(turmaId, disciplina!.Id, data);
        return new(await MontarRespostaAsync(turma!, disciplina, data, conteudo));
    }

    public async Task<ConteudoQueryResult<DiarioResponse>> GetDiarioAsync(
        UsuarioAtual usuario, int turmaId, int? disciplinaId, DateOnly? de, DateOnly? ate)
    {
        var (turma, disciplina, erro) = await ValidarAcessoAsync(usuario, turmaId, disciplinaId, disciplinaOpcionalNaDiaria: true);
        if (erro is not null)
            return new(default, erro.Message, erro.Error);

        var inicio = de ?? turma!.AnoLetivo.DataInicio;
        var fim = ate ?? turma!.AnoLetivo.DataTermino;
        if (inicio > fim)
            return new(default, "A data inicial deve ser anterior ou igual à data final.", ConteudoResultError.Validation);

        // Frequência diária: a chamada é da turma no dia (sem disciplina) e vale para os conteúdos de todas as disciplinas.
        var diaria = Diaria(turma!);
        var chamadas = await _chamadaRepository.ListAsync(turmaId, diaria ? null : disciplina!.Id, inicio, fim);
        var conteudos = await _conteudoRepository.ListAsync(turmaId, diaria ? null : disciplina!.Id, inicio, fim);

        var chamadasPorData = chamadas.ToDictionary(c => c.Data);
        var conteudosPorData = conteudos.ToLookup(c => c.Data);

        var dias = chamadasPorData.Keys.Concat(conteudosPorData.Select(g => g.Key))
            .Distinct()
            .OrderByDescending(d => d)
            .Select(data =>
            {
                chamadasPorData.TryGetValue(data, out var chamada);
                var doDia = conteudosPorData[data].ToList();

                // RN01: frequência sem conteúdo ministrado e vice-versa.
                var (pendencia, alerta) = (chamada is not null, doDia.Count > 0) switch
                {
                    (true, false) => (DiarioPendencia.SemConteudo, AlertaSemConteudo),
                    (false, true) => (DiarioPendencia.SemFrequencia, AlertaSemFrequencia),
                    _ => ((string?)null, (string?)null),
                };

                return new DiarioDiaResponse(
                    data,
                    chamada is null ? null : new DiarioFrequenciaResponse(
                        chamada.Id,
                        chamada.QuantidadeAulas,
                        chamada.Registros.Count(r => r.Situacao == ChamadaAluno.SituacaoPresente),
                        chamada.Registros.Count(r => r.Situacao == ChamadaAluno.SituacaoFalta),
                        chamada.Registros.Count(r => r.Situacao == ChamadaAluno.SituacaoFaltaJustificada)),
                    doDia.Select(c => new DiarioConteudoResponse(
                        c.Id, c.DisciplinaId, c.Disciplina.Nome, c.Descricao, MapHabilidades(c))).ToList(),
                    pendencia,
                    alerta);
            })
            .ToList();

        return new(new DiarioResponse(turma!.EtapaEnsino.TipoFrequencia, inicio, fim, dias.Count(d => d.Pendencia is not null), dias));
    }

    public async Task<ConteudoQueryResult<IEnumerable<HabilidadeResumoResponse>>> GetSugestoesAsync(
        UsuarioAtual usuario, int turmaId, int? disciplinaId, string? busca)
    {
        var (turma, disciplina, erro) = await ValidarAcessoAsync(usuario, turmaId, disciplinaId);
        if (erro is not null)
            return new(default, erro.Message, erro.Error);

        var habilidades = await _habilidadeRepository.GetSugestoesAsync(turma!.EtapaEnsinoId, disciplina!.Id, busca);
        return new(habilidades.Select(h => new HabilidadeResumoResponse(h.Id, h.Codigo, h.Descricao)).ToList());
    }

    public async Task<ConteudoCommandResult> CreateAsync(UsuarioAtual usuario, ConteudoMinistradoRequest request)
    {
        var (turma, disciplina, erro) = await ValidarAcessoAsync(usuario, request.TurmaId, request.DisciplinaId);
        if (erro is not null)
            return erro;

        if (turma!.Status != Turma.StatusAtivo)
            return Invalid("Não é possível registrar conteúdo em uma turma inativa.");

        if (await _conteudoRepository.GetAsync(request.TurmaId, disciplina!.Id, request.Data) is not null)
        {
            return new(false,
                "Já existe um conteúdo registrado para esta turma, disciplina e data. Abra o registro existente para alterá-lo.",
                Error: ConteudoResultError.Conflict);
        }

        var (descricao, habilidadeIds, erroDados) = await ValidarDadosAsync(turma, request, habilidadesAtuais: []);
        if (erroDados is not null)
            return erroDados;

        await _conteudoRepository.AddAsync(new ConteudoMinistrado
        {
            TurmaId = request.TurmaId,
            DisciplinaId = disciplina.Id,
            Data = request.Data,
            Descricao = descricao!,
            RegistradoPorUsuarioId = usuario.UsuarioId,
            Habilidades = habilidadeIds!.Select(id => new ConteudoMinistradoHabilidade { HabilidadeBnccId = id }).ToList(),
        });

        var salvo = await _conteudoRepository.GetAsync(request.TurmaId, disciplina.Id, request.Data);
        return new(true, "Conteúdo registrado com sucesso!", await MontarRespostaAsync(turma, disciplina, request.Data, salvo));
    }

    public async Task<ConteudoCommandResult> UpdateAsync(UsuarioAtual usuario, int id, ConteudoMinistradoRequest request)
    {
        var conteudo = await _conteudoRepository.GetByIdAsync(id);
        if (conteudo is null)
            return NotFound();

        // Turma, disciplina e data identificam o registro e não mudam na edição.
        var (turma, disciplina, erro) = await ValidarAcessoAsync(usuario, conteudo.TurmaId, conteudo.DisciplinaId);
        if (erro is not null)
            return erro;

        request.TurmaId = conteudo.TurmaId;
        request.DisciplinaId = conteudo.DisciplinaId;
        request.Data = conteudo.Data;

        var (descricao, habilidadeIds, erroDados) = await ValidarDadosAsync(
            turma!, request, conteudo.Habilidades.Select(h => h.HabilidadeBnccId).ToHashSet());
        if (erroDados is not null)
            return erroDados;

        conteudo.Descricao = descricao!;
        conteudo.AtualizadoPorUsuarioId = usuario.UsuarioId;
        conteudo.UpdatedAt = DateTime.UtcNow;
        await _conteudoRepository.UpdateAsync(conteudo, habilidadeIds!);

        var salvo = await _conteudoRepository.GetByIdAsync(id);
        return new(true, "Conteúdo atualizado com sucesso!", await MontarRespostaAsync(turma!, disciplina!, conteudo.Data, salvo));
    }

    public async Task<ConteudoCommandResult> DeleteAsync(UsuarioAtual usuario, int id)
    {
        var conteudo = await _conteudoRepository.GetByIdAsync(id);
        if (conteudo is null)
            return NotFound();

        var (_, _, erro) = await ValidarAcessoAsync(usuario, conteudo.TurmaId, conteudo.DisciplinaId);
        if (erro is not null)
            return erro;

        await _conteudoRepository.DeleteAsync(id);
        return new(true, "Conteúdo excluído com sucesso!");
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

    private static bool Diaria(Turma turma) => turma.EtapaEnsino.TipoFrequencia == EtapaEnsino.FrequenciaDiaria;

    // Mesma regra da alocação de professor: disciplina sem etapas vinculadas vale para qualquer etapa.
    private static bool FazParteDaGrade(Disciplina disciplina, Turma turma)
        => disciplina.EtapasEnsino.Count == 0 || disciplina.EtapasEnsino.Any(e => e.EtapaEnsinoId == turma.EtapaEnsinoId);

    /// <summary>
    /// Na frequência diária o professor (polivalente) alocado em qualquer disciplina da turma registra o
    /// conteúdo de toda a grade, como na chamada (RF017 RN02); nas demais, só das disciplinas alocadas.
    /// </summary>
    private static bool PodeAcessar(HashSet<(int TurmaId, int DisciplinaId)>? escopo, Turma turma, Disciplina disciplina)
    {
        if (!FazParteDaGrade(disciplina, turma))
            return false;
        if (escopo is null)
            return true;

        return Diaria(turma)
            ? escopo.Any(e => e.TurmaId == turma.Id)
            : escopo.Contains((turma.Id, disciplina.Id));
    }

    /// <summary>
    /// Valida o acesso à turma e à disciplina. Na consulta do diário de uma turma de frequência diária a
    /// disciplina pode ser omitida (<paramref name="disciplinaOpcionalNaDiaria"/>).
    /// </summary>
    private async Task<(Turma? Turma, Disciplina? Disciplina, ConteudoCommandResult? Erro)> ValidarAcessoAsync(
        UsuarioAtual usuario, int turmaId, int? disciplinaId, bool disciplinaOpcionalNaDiaria = false)
    {
        var turma = await _turmaRepository.GetByIdAsync(turmaId);
        if (turma is null)
            return (null, null, new(false, "Turma não encontrada.", Error: ConteudoResultError.NotFound));

        var escopo = await GetEscopoAsync(usuario);

        if (disciplinaId is null)
        {
            if (!disciplinaOpcionalNaDiaria || !Diaria(turma))
                return (null, null, Invalid("Selecione a disciplina do conteúdo."));

            if (escopo is not null && !escopo.Any(e => e.TurmaId == turmaId))
                return (null, null, Forbidden());

            return (turma, null, null);
        }

        var disciplina = await _disciplinaRepository.GetByIdAsync(disciplinaId.Value);
        if (disciplina is null)
            return (null, null, new(false, "Disciplina não encontrada.", Error: ConteudoResultError.NotFound));

        if (escopo is not null && !PodeAcessar(escopo, turma, disciplina))
            return (null, null, Forbidden());

        if (escopo is null && !FazParteDaGrade(disciplina, turma))
            return (null, null, Invalid("A disciplina não faz parte da grade desta turma."));

        return (turma, disciplina, null);
    }

    private static string? ValidarData(Turma turma, DateOnly data)
    {
        if (data == default)
            return "Informe a data do conteúdo.";

        if (data > DateOnly.FromDateTime(DateTime.Today))
            return "A data do conteúdo não pode ser futura.";

        var ano = turma.AnoLetivo;
        if (data < ano.DataInicio || data > ano.DataTermino)
        {
            return $"A data do conteúdo deve estar dentro do ano letivo da turma " +
                   $"({ano.DataInicio:dd/MM/yyyy} a {ano.DataTermino:dd/MM/yyyy}).";
        }

        return null;
    }

    /// <summary>EX01: motivo do bloqueio da data pelo Calendário Letivo, ou nulo se pode registrar.</summary>
    private async Task<string?> GetBloqueioAsync(Turma turma, DateOnly data)
        => CalendarioEfetivo.MotivoBloqueio(data, await _calendarioRepository.GetPublicadosAsync(turma.AnoLetivoId, turma.EscolaId));

    private async Task<(string? Descricao, List<int>? HabilidadeIds, ConteudoCommandResult? Erro)> ValidarDadosAsync(
        Turma turma, ConteudoMinistradoRequest request, HashSet<int> habilidadesAtuais)
    {
        var erroData = ValidarData(turma, request.Data);
        if (erroData is not null)
            return (null, null, Invalid(erroData));

        // EX01: dia sem aula no calendário publicado bloqueia o registro.
        var bloqueio = await GetBloqueioAsync(turma, request.Data);
        if (bloqueio is not null)
            return (null, null, Invalid(bloqueio));

        var descricao = request.Descricao?.Trim() ?? string.Empty;
        if (descricao.Length == 0)
            return (null, null, Invalid("Informe o conteúdo ministrado."));
        if (descricao.Length > ConteudoMinistrado.MaxDescricao)
            return (null, null, Invalid($"O conteúdo deve ter no máximo {ConteudoMinistrado.MaxDescricao} caracteres."));

        // A BNCC é sugestão: não exige que a habilidade seja da etapa/disciplina da turma, mas ela precisa existir
        // e estar ativa (uma já vinculada ao registro pode ter sido inativada depois e continua valendo).
        var ids = (request.HabilidadesIds ?? []).Distinct().ToList();
        if (ids.Count > 0)
        {
            var encontradas = await _habilidadeRepository.GetByIdsAsync(ids);
            if (encontradas.Count != ids.Count)
                return (null, null, Invalid("Habilidade da BNCC não encontrada."));

            var inativa = encontradas.FirstOrDefault(h => !h.Ativa && !habilidadesAtuais.Contains(h.Id));
            if (inativa is not null)
                return (null, null, Invalid($"A habilidade {inativa.Codigo} está inativa e não pode ser vinculada."));
        }

        return (descricao, ids, null);
    }

    private async Task<ConteudoMinistradoResponse> MontarRespostaAsync(Turma turma, Disciplina disciplina, DateOnly data, ConteudoMinistrado? conteudo)
    {
        var bloqueio = await GetBloqueioAsync(turma, data);
        if (conteudo is null)
            return new(null, turma.Id, disciplina.Id, disciplina.Nome, data, null, [], null, null, null, null, bloqueio);

        var emails = await _chamadaRepository.GetEmailsUsuariosAsync(
            new[] { conteudo.RegistradoPorUsuarioId, conteudo.AtualizadoPorUsuarioId ?? 0 }.Where(id => id > 0));

        return new(
            conteudo.Id, turma.Id, disciplina.Id, disciplina.Nome, data, conteudo.Descricao, MapHabilidades(conteudo),
            emails.GetValueOrDefault(conteudo.RegistradoPorUsuarioId), conteudo.CreatedAt,
            conteudo.AtualizadoPorUsuarioId is int atualizadoPor ? emails.GetValueOrDefault(atualizadoPor) : null,
            conteudo.UpdatedAt,
            bloqueio);
    }

    private static List<HabilidadeResumoResponse> MapHabilidades(ConteudoMinistrado conteudo)
        => conteudo.Habilidades
            .Select(h => new HabilidadeResumoResponse(h.HabilidadeBnccId, h.HabilidadeBncc.Codigo, h.HabilidadeBncc.Descricao, h.HabilidadeBncc.Ativa))
            .OrderBy(h => h.Codigo)
            .ToList();

    private static ConteudoCommandResult Invalid(string message)
        => new(false, message, Error: ConteudoResultError.Validation);

    private static ConteudoCommandResult NotFound()
        => new(false, "Conteúdo não encontrado.", Error: ConteudoResultError.NotFound);

    private static ConteudoCommandResult Forbidden()
        => new(false, "Você só pode acessar o conteúdo das turmas e disciplinas em que está alocado.", Error: ConteudoResultError.Forbidden);
}
