using System.Globalization;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Notas;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Notas;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class NotaService : INotaService
{
    private const string StatusTransferido = "TRANSFERIDO";
    private const string StatusRemanejado = "REMANEJADO";
    private const string StatusDesenturmado = "DESENTURMADO";

    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly IAvaliacaoRepository _avaliacaoRepository;
    private readonly IRegraAvaliacaoRepository _regraRepository;
    private readonly ITurmaRepository _turmaRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly IAnoLetivoRepository _anoLetivoRepository;
    private readonly IProfessorRepository _professorRepository;
    private readonly IProfessorAlocacaoRepository _alocacaoRepository;

    public NotaService(
        IAvaliacaoRepository avaliacaoRepository,
        IRegraAvaliacaoRepository regraRepository,
        ITurmaRepository turmaRepository,
        IDisciplinaRepository disciplinaRepository,
        IAnoLetivoRepository anoLetivoRepository,
        IProfessorRepository professorRepository,
        IProfessorAlocacaoRepository alocacaoRepository)
    {
        _avaliacaoRepository = avaliacaoRepository;
        _regraRepository = regraRepository;
        _turmaRepository = turmaRepository;
        _disciplinaRepository = disciplinaRepository;
        _anoLetivoRepository = anoLetivoRepository;
        _professorRepository = professorRepository;
        _alocacaoRepository = alocacaoRepository;
    }

    public async Task<IEnumerable<NotaTurmaResponse>> GetTurmasAsync(UsuarioAtual usuario)
    {
        var escopo = await GetEscopoAsync(usuario);
        var turmas = await _turmaRepository.GetAllAsync();
        var disciplinas = (await _disciplinaRepository.GetAllAsync()).Where(d => d.Ativa).ToList();
        var anos = (await _anoLetivoRepository.GetAllAsync()).ToDictionary(a => a.Id);
        var regrasPorEtapa = (await _regraRepository.GetAllAsync())
            .SelectMany(r => r.Etapas.Select(e => (EtapaId: e.Id, Regra: r)))
            .ToDictionary(x => x.EtapaId, x => x.Regra);
        var padrao = RegraAvaliacao.PadraoDoSistema();

        // Turmas inativas continuam listadas para consulta das notas já lançadas.
        return turmas
            .Select(turma =>
            {
                var permitidas = disciplinas
                    .Where(d => escopo is null ? d.FazParteDaGrade(turma) : escopo.Contains((turma.Id, d.Id)))
                    .OrderBy(d => d.Nome)
                    .Select(d => new NotaDisciplinaResponse(d.Id, d.Nome))
                    .ToList();

                var periodos = anos.TryGetValue(turma.AnoLetivoId, out var ano)
                    ? ano.Periodos.OrderBy(p => p.Numero).Select(MapPeriodo).ToList()
                    : [];

                return new NotaTurmaResponse(
                    turma.Id, turma.NomeCompleto, turma.Escola.Nome, turma.AnoLetivo.AnoReferencia, turma.Turno,
                    turma.Status == Turma.StatusAtivo, permitidas, periodos,
                    MapRegra(regrasPorEtapa.GetValueOrDefault(turma.EtapaEnsinoId) ?? padrao));
            })
            .Where(t => t.Disciplinas.Count > 0 && t.Periodos.Count > 0)
            .OrderByDescending(t => t.Ativa)
            .ThenByDescending(t => t.AnoReferencia)
            .ThenBy(t => t.TurmaNome)
            .ToList();
    }

    public async Task<NotaResult<NotasPeriodoResponse>> GetPeriodoAsync(UsuarioAtual usuario, int turmaId, int disciplinaId, int periodoId)
    {
        var (contexto, erro) = await CarregarContextoAsync(usuario, turmaId, disciplinaId, periodoId);
        return erro is not null
            ? Falha<NotasPeriodoResponse>(erro)
            : new(await MontarPeriodoAsync(contexto!));
    }

    public async Task<NotaResult<MediasResponse>> GetMediasAsync(UsuarioAtual usuario, int turmaId, int disciplinaId)
    {
        var (turma, erro) = await ValidarAcessoAsync(usuario, turmaId, disciplinaId);
        if (erro is not null)
            return Falha<MediasResponse>(erro);

        var periodos = await ListarPeriodosAsync(turma!);
        var regra = await ObterRegraAsync(turma!);
        var avaliacoes = await _avaliacaoRepository.ListAsync(turmaId, disciplinaId);
        var enturmacoes = await _avaliacaoRepository.GetEnturmacoesAsync(turmaId, turma!.AnoLetivo.DataInicio, turma.AnoLetivo.DataTermino);
        var valores = IndexarNotas(avaliacoes);

        var linhas = ListarAlunos(enturmacoes, avaliacoes)
            .Select(item =>
            {
                var porPeriodo = periodos
                    .Select(p =>
                    {
                        var resultado = CalcularPeriodo(regra, avaliacoes.Where(a => a.PeriodoAvaliativoId == p.Id), item.Aluno.Id, valores);
                        return new NotaDoPeriodoResponse(p.Id, resultado.Nota, resultado.Pendentes);
                    })
                    .ToList();
                var final = CalculoNotas.CalcularFinal(regra, porPeriodo.Select(p => p.Nota).ToList());

                return new AlunoMediaResponse(
                    item.Aluno.Id, item.Aluno.Matricula, item.Aluno.Nome, item.Aluno.Status == Aluno.StatusInativo,
                    porPeriodo, final.Media, final.Completa, final.Situacao);
            })
            .ToList();

        return new(new MediasResponse(turmaId, disciplinaId, MapRegra(regra), periodos.Select(MapPeriodo).ToList(), linhas));
    }

    public async Task<NotaResult<AvaliacaoResponse>> SalvarAvaliacaoAsync(UsuarioAtual usuario, int? id, AvaliacaoRequest request)
    {
        Avaliacao? existente = null;
        if (id is not null)
        {
            existente = await _avaliacaoRepository.GetByIdAsync(id.Value);
            if (existente is null)
                return NaoEncontrada<AvaliacaoResponse>();

            // Turma, disciplina e período identificam a avaliação e não mudam na edição.
            request.TurmaId = existente.TurmaId;
            request.DisciplinaId = existente.DisciplinaId;
            request.PeriodoAvaliativoId = existente.PeriodoAvaliativoId;
        }

        var (contexto, erro) = await CarregarContextoAsync(usuario, request.TurmaId, request.DisciplinaId, request.PeriodoAvaliativoId);
        if (erro is not null)
            return Falha<AvaliacaoResponse>(erro);

        var (turma, periodo, regra) = (contexto!.Turma, contexto.Periodo, contexto.Regra);
        if (turma.Status != Turma.StatusAtivo)
            return Invalida<AvaliacaoResponse>("Não é possível alterar as avaliações de uma turma inativa.");

        var bloqueio = BloqueioDoPrazo(turma, periodo);
        if (bloqueio is not null)
            return Invalida<AvaliacaoResponse>(bloqueio);

        var nome = (request.Nome ?? string.Empty).Trim();
        if (nome.Length == 0)
            return Invalida<AvaliacaoResponse>("Informe o nome da avaliação.");
        if (nome.Length > Avaliacao.MaxNome)
            return Invalida<AvaliacaoResponse>($"O nome da avaliação deve ter no máximo {Avaliacao.MaxNome} caracteres.");

        var tipo = (request.Tipo ?? string.Empty).Trim().ToUpperInvariant();
        if (!Avaliacao.Tipos.Contains(tipo))
            return Invalida<AvaliacaoResponse>("Tipo de avaliação inválido. Use Prova, Trabalho, Atividade, Participação, Outro ou Recuperação.");

        if (request.Data is DateOnly data && (data < periodo.DataInicio || data > periodo.DataTermino))
        {
            return Invalida<AvaliacaoResponse>(
                $"A data da avaliação deve estar dentro do período {periodo.Nome} " +
                $"({periodo.DataInicio:dd/MM/yyyy} a {periodo.DataTermino:dd/MM/yyyy}).");
        }

        var outras = (await _avaliacaoRepository.ListAsync(turma.Id, contexto.DisciplinaId, periodo.Id))
            .Where(a => a.Id != id)
            .ToList();

        var peso = 1m;
        decimal? valorMaximo = null;
        if (tipo == Avaliacao.TipoRecuperacao)
        {
            if (!regra.PermiteRecuperacao)
                return Invalida<AvaliacaoResponse>("A regra de avaliação desta turma não prevê recuperação.");
            if (outras.Any(a => a.EhRecuperacao))
                return Conflito<AvaliacaoResponse>("Já existe uma recuperação neste período. Lance as notas na recuperação existente.");
        }
        else if (regra.CalculoNotaPeriodo == RegraAvaliacao.CalculoSoma)
        {
            if (request.ValorMaximo is not decimal valor || valor <= 0)
                return Invalida<AvaliacaoResponse>("Informe quantos pontos a avaliação vale.");
            if (MaisDeDuasCasas(valor))
                return Invalida<AvaliacaoResponse>("O valor da avaliação aceita no máximo duas casas decimais.");

            var soma = outras.Where(a => !a.EhRecuperacao).Sum(a => a.ValorMaximo ?? 0m) + valor;
            if (soma > regra.NotaMaxima)
            {
                return Invalida<AvaliacaoResponse>(
                    $"A soma dos pontos das avaliações do período ficaria em {Formatar(soma)}, " +
                    $"acima da nota máxima ({Formatar(regra.NotaMaxima)}).");
            }

            valorMaximo = valor;
        }
        else
        {
            peso = request.Peso ?? 1m;
            if (peso <= 0 || peso > Avaliacao.PesoMaximo)
                return Invalida<AvaliacaoResponse>($"O peso da avaliação deve ser maior que zero e no máximo {Formatar(Avaliacao.PesoMaximo)}.");
            if (MaisDeDuasCasas(peso))
                return Invalida<AvaliacaoResponse>("O peso aceita no máximo duas casas decimais.");
        }

        var avaliacao = new Avaliacao
        {
            Id = id ?? 0,
            TurmaId = turma.Id,
            DisciplinaId = contexto.DisciplinaId,
            PeriodoAvaliativoId = periodo.Id,
            Nome = nome,
            Tipo = tipo,
            Data = request.Data,
            Peso = peso,
            ValorMaximo = valorMaximo,
        };

        if (existente is null)
        {
            avaliacao.RegistradoPorUsuarioId = usuario.UsuarioId;
            await _avaliacaoRepository.AddAsync(avaliacao);
        }
        else
        {
            var maximo = NotaMaximaDa(avaliacao, regra);
            var acima = existente.Notas.Count(n => n.Valor > maximo);
            if (acima > 0)
            {
                return Invalida<AvaliacaoResponse>(
                    $"Há {acima} nota(s) lançada(s) acima de {Formatar(maximo)} nesta avaliação. Corrija as notas antes de reduzir o valor.");
            }

            avaliacao.AtualizadoPorUsuarioId = usuario.UsuarioId;
            avaliacao.UpdatedAt = DateTime.UtcNow;
            await _avaliacaoRepository.UpdateAsync(avaliacao);
        }

        var salva = await _avaliacaoRepository.GetByIdAsync(avaliacao.Id) ?? avaliacao;
        return new(MapAvaliacao(salva), existente is null ? "Avaliação cadastrada com sucesso!" : "Avaliação atualizada com sucesso!");
    }

    public async Task<NotaResult<bool>> ExcluirAvaliacaoAsync(UsuarioAtual usuario, int id)
    {
        var avaliacao = await _avaliacaoRepository.GetByIdAsync(id);
        if (avaliacao is null)
            return NaoEncontrada<bool>();

        var (turma, erro) = await ValidarAcessoAsync(usuario, avaliacao.TurmaId, avaliacao.DisciplinaId);
        if (erro is not null)
            return Falha<bool>(erro);

        if (turma!.Status != Turma.StatusAtivo)
            return Invalida<bool>("Não é possível alterar as avaliações de uma turma inativa.");

        var periodoDaAvaliacao = (await ListarPeriodosAsync(turma)).FirstOrDefault(p => p.Id == avaliacao.PeriodoAvaliativoId);
        var bloqueioPrazo = periodoDaAvaliacao is null ? null : BloqueioDoPrazo(turma, periodoDaAvaliacao);
        if (bloqueioPrazo is not null)
            return Invalida<bool>(bloqueioPrazo);

        await _avaliacaoRepository.DeleteAsync(id);
        return new(true, "Avaliação excluída com sucesso!");
    }

    public async Task<NotaResult<NotasPeriodoResponse>> LancarNotasAsync(UsuarioAtual usuario, LancamentoNotasRequest request)
    {
        var (contexto, erro) = await CarregarContextoAsync(usuario, request.TurmaId, request.DisciplinaId, request.PeriodoAvaliativoId);
        if (erro is not null)
            return Falha<NotasPeriodoResponse>(erro);

        if (contexto!.Turma.Status != Turma.StatusAtivo)
            return Invalida<NotasPeriodoResponse>("Não é possível lançar notas em uma turma inativa.");

        var bloqueio = BloqueioDoPrazo(contexto.Turma, contexto.Periodo);
        if (bloqueio is not null)
            return Invalida<NotasPeriodoResponse>(bloqueio);

        var itens = request.Notas ?? new List<NotaLancamentoRequest>();
        if (itens.Count == 0)
            return Invalida<NotasPeriodoResponse>("Nenhuma nota informada.");

        if (itens.GroupBy(n => (n.AvaliacaoId, n.AlunoId)).Any(g => g.Count() > 1))
            return Invalida<NotasPeriodoResponse>("Uma mesma nota foi informada mais de uma vez.");

        var avaliacoes = (await _avaliacaoRepository.ListAsync(contexto.Turma.Id, contexto.DisciplinaId, contexto.Periodo.Id))
            .ToDictionary(a => a.Id);
        var enturmacoes = await _avaliacaoRepository.GetEnturmacoesAsync(
            contexto.Turma.Id, contexto.Periodo.DataInicio, contexto.Periodo.DataTermino);
        var alunos = ListarAlunos(enturmacoes, avaliacoes.Values).ToDictionary(a => a.Aluno.Id);

        foreach (var item in itens)
        {
            if (!avaliacoes.TryGetValue(item.AvaliacaoId, out var avaliacao))
                return Invalida<NotasPeriodoResponse>("A avaliação informada não pertence a esta turma, disciplina e período.");

            if (!alunos.TryGetValue(item.AlunoId, out var naLista))
                return Invalida<NotasPeriodoResponse>("Há nota de um aluno que não está na turma neste período.");

            // RN03: quem saiu da turma (transferido, remanejado, desenturmado) mantém as notas salvas, só para leitura.
            if (naLista.StatusSaida is not null)
            {
                var salva = avaliacao.Notas.FirstOrDefault(n => n.AlunoId == item.AlunoId)?.Valor;
                if (salva != item.Valor)
                {
                    return Invalida<NotasPeriodoResponse>(
                        $"{naLista.Aluno.Nome} deixou a turma ({RotuloSaida(naLista.StatusSaida)}): as notas lançadas ficam somente leitura.");
                }

                continue;
            }

            if (item.Valor is not decimal valor)
                continue;

            // RN01 / EX01: a nota fica entre zero e o valor máximo da avaliação.
            var maximo = NotaMaximaDa(avaliacao, contexto.Regra);
            if (valor < 0)
                return Invalida<NotasPeriodoResponse>("A nota informada não pode ser menor que zero.");
            if (valor > maximo)
                return Invalida<NotasPeriodoResponse>($"A nota informada excede o valor máximo permitido ({FormatarNota(maximo)}).");

            if (MaisDeDuasCasas(valor))
                return Invalida<NotasPeriodoResponse>("As notas aceitam no máximo duas casas decimais.");
        }

        await _avaliacaoRepository.SalvarNotasAsync(
            itens.Select(n => new NotaInformada(n.AvaliacaoId, n.AlunoId, n.Valor)).ToList(), usuario.UsuarioId);

        return new(await MontarPeriodoAsync(contexto), "Notas salvas com sucesso!");
    }

    public async Task<NotaResult<MapaNotasResponse>> GetMapaDaTurmaAsync(int turmaId)
    {
        var turma = await _turmaRepository.GetByIdAsync(turmaId);
        if (turma is null)
            return new(default, "Turma não encontrada.", NotaResultError.NotFound);

        var regra = await ObterRegraAsync(turma);
        var periodos = await ListarPeriodosAsync(turma);
        var avaliacoes = await _avaliacaoRepository.ListByTurmaAsync(turmaId);
        var disciplinas = (await _disciplinaRepository.GetAllAsync())
            .Where(d => (d.Ativa && d.FazParteDaGrade(turma)) || avaliacoes.Any(a => a.DisciplinaId == d.Id))
            .OrderBy(d => d.Nome)
            .ToList();
        var enturmacoes = await _avaliacaoRepository.GetEnturmacoesAsync(turmaId, turma.AnoLetivo.DataInicio, turma.AnoLetivo.DataTermino);
        var valores = IndexarNotas(avaliacoes);

        var linhas = ListarAlunos(enturmacoes, avaliacoes)
            .Select(item =>
            {
                var medias = disciplinas
                    .Select(d => CalculoNotas.CalcularFinal(regra, periodos
                        .Select(p => CalcularPeriodo(regra,
                            avaliacoes.Where(a => a.DisciplinaId == d.Id && a.PeriodoAvaliativoId == p.Id), item.Aluno.Id, valores).Nota)
                        .ToList()).Media)
                    .ToList();

                return new MapaNotasAluno(item.Aluno.Matricula, item.Aluno.Nome, medias,
                    medias.Count(m => CalculoNotas.AbaixoDaMedia(regra, m)));
            })
            .ToList();

        return new(new MapaNotasResponse(
            MapRegra(regra), disciplinas.Select(d => new NotaDisciplinaResponse(d.Id, d.Nome)).ToList(), linhas));
    }

    // ---------- Montagem ----------

    private sealed record Contexto(Turma Turma, int DisciplinaId, PeriodoAvaliativo Periodo, RegraAvaliacao Regra);

    private sealed record Erro(string Mensagem, NotaResultError Tipo);

    private sealed record AlunoDaLista(Aluno Aluno, DateOnly? SaiuEm, string? StatusSaida = null);

    private async Task<(Contexto? Contexto, Erro? Erro)> CarregarContextoAsync(UsuarioAtual usuario, int turmaId, int disciplinaId, int periodoId)
    {
        var (turma, erro) = await ValidarAcessoAsync(usuario, turmaId, disciplinaId);
        if (erro is not null)
            return (null, erro);

        var periodo = (await ListarPeriodosAsync(turma!)).FirstOrDefault(p => p.Id == periodoId);
        if (periodo is null)
            return (null, new Erro("Período avaliativo não encontrado para o ano letivo da turma.", NotaResultError.NotFound));

        return (new Contexto(turma!, disciplinaId, periodo, await ObterRegraAsync(turma!)), null);
    }

    private async Task<NotasPeriodoResponse> MontarPeriodoAsync(Contexto contexto)
    {
        var (turma, periodo, regra) = (contexto.Turma, contexto.Periodo, contexto.Regra);
        var avaliacoes = (await _avaliacaoRepository.ListAsync(turma.Id, contexto.DisciplinaId, periodo.Id))
            .OrderBy(a => a.EhRecuperacao)
            .ThenBy(a => a.Data is null)
            .ThenBy(a => a.Data)
            .ThenBy(a => a.Id)
            .ToList();
        var enturmacoes = await _avaliacaoRepository.GetEnturmacoesAsync(turma.Id, periodo.DataInicio, periodo.DataTermino);
        var valores = IndexarNotas(avaliacoes);

        var linhas = ListarAlunos(enturmacoes, avaliacoes)
            .Select(item =>
            {
                var resultado = CalcularPeriodo(regra, avaliacoes, item.Aluno.Id, valores);
                var notas = avaliacoes
                    .Where(a => valores.ContainsKey((a.Id, item.Aluno.Id)))
                    .Select(a => new NotaValorResponse(a.Id, valores[(a.Id, item.Aluno.Id)]))
                    .ToList();

                return new AlunoNotasPeriodoResponse(
                    item.Aluno.Id, item.Aluno.Matricula, item.Aluno.Nome, item.SaiuEm, item.Aluno.Status == Aluno.StatusInativo,
                    notas, resultado.Media, resultado.Recuperacao, resultado.Nota, resultado.Pendentes,
                    CalculoNotas.AbaixoDaMedia(regra, resultado.Nota), item.StatusSaida, resultado.RecuperacaoAplicada);
            })
            .ToList();

        return new NotasPeriodoResponse(
            turma.Id, contexto.DisciplinaId, MapPeriodo(periodo), turma.Status == Turma.StatusAtivo, MapRegra(regra),
            avaliacoes.Select(MapAvaliacao).ToList(), linhas, BloqueioDoPrazo(turma, periodo));
    }

    /// <summary>
    /// Alunos que passaram pela turma no intervalo e os que já têm nota nas avaliações (ex.: remanejados
    /// depois). Inativos sem nota ficam de fora.
    /// </summary>
    private static List<AlunoDaLista> ListarAlunos(IEnumerable<AlunoTurma> enturmacoes, IEnumerable<Avaliacao> avaliacoes)
    {
        var comNota = avaliacoes
            .SelectMany(a => a.Notas)
            .Where(n => n.Aluno is not null)
            .GroupBy(n => n.AlunoId)
            .ToDictionary(g => g.Key, g => g.First().Aluno);

        var lista = enturmacoes
            .GroupBy(e => e.AlunoId)
            .Select(g =>
            {
                var aluno = g.First().Aluno;
                if (g.Any(e => e.DataFim is null))
                    return new AlunoDaLista(aluno, null);

                var ultima = g.OrderByDescending(e => e.DataFim).First();
                return new AlunoDaLista(aluno, ultima.DataFim, StatusDeSaida(aluno, ultima.MotivoDesenturmacao));
            })
            .Where(a => a.Aluno.Status != Aluno.StatusInativo || comNota.ContainsKey(a.Aluno.Id))
            .ToList();

        var listados = lista.Select(a => a.Aluno.Id).ToHashSet();
        // Sem enturmação no intervalo: só aparece pelas notas já salvas, que ficam somente leitura.
        lista.AddRange(comNota.Values.Where(a => !listados.Contains(a.Id)).Select(a => new AlunoDaLista(a, null, StatusDeSaida(a, null))));

        return lista.OrderBy(a => a.Aluno.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static string StatusDeSaida(Aluno aluno, string? motivoDesenturmacao)
    {
        if (aluno.Status == Aluno.StatusTransferido || motivoDesenturmacao == AlunoTurma.MotivoTransferencia)
            return StatusTransferido;

        return motivoDesenturmacao == AlunoTurma.MotivoRemanejamento ? StatusRemanejado : StatusDesenturmado;
    }

    private static Dictionary<(int AvaliacaoId, int AlunoId), decimal> IndexarNotas(IEnumerable<Avaliacao> avaliacoes)
        => avaliacoes.SelectMany(a => a.Notas).ToDictionary(n => (n.AvaliacaoId, n.AlunoId), n => n.Valor);

    private static ResultadoPeriodo CalcularPeriodo(
        RegraAvaliacao regra, IEnumerable<Avaliacao> avaliacoes, int alunoId, IReadOnlyDictionary<(int, int), decimal> valores)
    {
        var itens = avaliacoes
            .Select(a => new ItemNotaPeriodo(
                a.Peso,
                valores.TryGetValue((a.Id, alunoId), out var valor) ? valor : null,
                a.EhRecuperacao))
            .ToList();

        return CalculoNotas.CalcularPeriodo(regra, itens);
    }

    /// <summary>Na regra de soma, cada avaliação vale até os seus pontos; nas demais (e na recuperação), até a nota máxima.</summary>
    private static decimal NotaMaximaDa(Avaliacao avaliacao, RegraAvaliacao regra)
        => regra.CalculoNotaPeriodo == RegraAvaliacao.CalculoSoma && !avaliacao.EhRecuperacao && avaliacao.ValorMaximo is decimal pontos
            ? pontos
            : regra.NotaMaxima;

    private async Task<List<PeriodoAvaliativo>> ListarPeriodosAsync(Turma turma)
    {
        var ano = await _anoLetivoRepository.GetByIdAsync(turma.AnoLetivoId);
        return ano?.Periodos.OrderBy(p => p.Numero).ToList() ?? [];
    }

    private async Task<RegraAvaliacao> ObterRegraAsync(Turma turma)
        => await _regraRepository.GetByEtapaAsync(turma.EtapaEnsinoId) ?? RegraAvaliacao.PadraoDoSistema();

    // ---------- Acesso ----------

    /// <summary>
    /// Mesma regra da chamada: nulo = acesso a todas as turmas (gestão e Administrador global); para o
    /// usuário vinculado a um professor, só os pares turma/disciplina das alocações ativas dele.
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

    private async Task<(Turma? Turma, Erro? Erro)> ValidarAcessoAsync(UsuarioAtual usuario, int turmaId, int disciplinaId)
    {
        var turma = await _turmaRepository.GetByIdAsync(turmaId);
        if (turma is null)
            return (null, new Erro("Turma não encontrada.", NotaResultError.NotFound));

        var disciplina = await _disciplinaRepository.GetByIdAsync(disciplinaId);
        if (disciplina is null)
            return (null, new Erro("Disciplina não encontrada.", NotaResultError.NotFound));

        var escopo = await GetEscopoAsync(usuario);
        if (escopo is not null && !escopo.Contains((turmaId, disciplinaId)))
        {
            return (null, new Erro(
                "Você só pode acessar as notas das turmas e disciplinas em que está alocado.",
                NotaResultError.Forbidden));
        }

        if (escopo is null && !disciplina.FazParteDaGrade(turma))
            return (null, new Erro("A disciplina não faz parte da grade desta turma.", NotaResultError.Validation));

        return (turma, null);
    }

    // ---------- Mapeamento e resultados ----------

    private static bool MaisDeDuasCasas(decimal valor) => decimal.Round(valor, 2) != valor;

    private static string Formatar(decimal valor) => valor.ToString("0.##", PtBr);

    /// <summary>Valor máximo no formato da mensagem de EX01: sempre com uma casa decimal (10.0).</summary>
    private static string FormatarNota(decimal valor) => valor.ToString("0.0#", CultureInfo.InvariantCulture);

    private static NotaPeriodoResponse MapPeriodo(PeriodoAvaliativo p)
        => new(p.Id, p.Nome, p.Numero, p.DataInicio, p.DataTermino, p.PrazoLancamentoNotas);

    /// <summary>EX02: mensagem de bloqueio se o prazo de lançamento de notas do período expirou; nulo se ainda está aberto.</summary>
    private static string? BloqueioDoPrazo(Turma turma, PeriodoAvaliativo periodo)
    {
        if (periodo.PrazoLancamentoNotas is not DateOnly prazo || prazo >= DateOnly.FromDateTime(DateTime.Today))
            return null;

        var termo = turma.AnoLetivo.TipoPeriodo switch
        {
            AnoLetivo.TipoBimestral => "bimestre",
            AnoLetivo.TipoTrimestral => "trimestre",
            AnoLetivo.TipoSemestral => "semestre",
            _ => "período",
        };
        return $"Lançamento de notas bloqueado para este {termo}. Prazo encerrado em {prazo:dd/MM/yyyy}.";
    }

    private static string RotuloSaida(string statusSaida) => statusSaida switch
    {
        StatusTransferido => "Transferido",
        StatusRemanejado => "Remanejado",
        _ => "Desenturmado",
    };

    private static RegraAvaliacaoResumoResponse MapRegra(RegraAvaliacao r)
        => new(r.Id == 0 ? null : r.Id, r.Nome, r.NotaMaxima, r.MediaAprovacao, r.CasasDecimais, r.CalculoNotaPeriodo, r.PermiteRecuperacao, r.SubstituicaoRecuperacao);

    private static AvaliacaoResponse MapAvaliacao(Avaliacao a)
        => new(a.Id, a.PeriodoAvaliativoId, a.Nome, a.Tipo, a.Data, a.Peso, a.ValorMaximo, a.EhRecuperacao, a.Notas.Count);

    private static NotaResult<T> Falha<T>(Erro erro) => new(default, erro.Mensagem, erro.Tipo);

    private static NotaResult<T> Invalida<T>(string mensagem) => new(default, mensagem, NotaResultError.Validation);

    private static NotaResult<T> Conflito<T>(string mensagem) => new(default, mensagem, NotaResultError.Conflict);

    private static NotaResult<T> NaoEncontrada<T>() => new(default, "Avaliação não encontrada.", NotaResultError.NotFound);
}
