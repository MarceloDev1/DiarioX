using DiarioX.Server.Application.Dashboard;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Dashboard;

/// <summary>
/// Consultas da página inicial. Os filtros globais do AppDbContext já restringem tudo à instituição e às
/// escolas do usuário; as agregações são projetadas para rodar no banco.
/// </summary>
public class DashboardConsultas : IDashboardConsultas
{
    private readonly AppDbContext _context;

    public DashboardConsultas(AppDbContext context)
    {
        _context = context;
    }

    public Task<int?> ObterProfessorIdDoUsuarioAsync(int usuarioId)
        => _context.Professores.AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

    public async Task<IndicadoresLinha> ObterIndicadoresAsync(ParametrosIndicadores p)
    {
        var hoje = p.Hoje;

        // Cada IQueryable abaixo vira uma subconsulta escalar do mesmo SELECT: uma só ida ao banco.
        var alunosAtivos = AlunosEnturmadosEm(hoje);
        var alunosNaComparacao = AlunosEnturmadosEm(p.ComparacaoAlunos);
        var aguardando = _context.Alunos.Where(a => a.Status == Aluno.StatusAtivoAguardandoEnturmacao);

        var turmasAtivas = _context.Turmas.Where(t => t.Status == Turma.StatusAtivo);
        var professoresAlocados = _context.ProfessorAlocacoes
            .Where(pa => pa.Ativa && pa.Turma.Status == Turma.StatusAtivo)
            .Select(pa => pa.ProfessorId)
            .Distinct();
        var professoresAtivos = _context.Professores.Where(x => x.Situacao == Professor.StatusAtivo);

        var registros = RegistrosDoPeriodo(p.FrequenciaDe, hoje, p.ProfessorId);
        var presencas = registros.Where(r => r.Situacao == ChamadaAluno.SituacaoPresente);
        var infrequentes = registros
            .Where(r => r.Aluno.Status != Aluno.StatusInativo)
            .GroupBy(r => r.AlunoId)
            .Select(g => new
            {
                Aulas = g.Sum(r => r.Chamada.QuantidadeAulas),
                // Falta justificada continua sendo ausência, como no cálculo da frequência da chamada.
                Presencas = g.Sum(r => r.Situacao == ChamadaAluno.SituacaoPresente ? r.Chamada.QuantidadeAulas : 0),
            })
            .Where(x => x.Aulas >= p.MinimoAulasInfrequencia && x.Presencas * 100m < p.FrequenciaMinima * x.Aulas);

        var semChamada = AlocacoesSemChamada(hoje, p.SemChamadaDesde, p.ProfessorId);

        var linha = await _context.Tenants.AsNoTracking()
            .Where(t => t.Id == _context.CurrentTenantId)
            .Select(_ => new IndicadoresLinha(
                alunosAtivos.Count(),
                alunosNaComparacao.Count(),
                aguardando.Count(),
                turmasAtivas.Count(),
                turmasAtivas.Select(t => t.EscolaId).Distinct().Count(),
                professoresAlocados.Count(),
                professoresAtivos.Count(),
                registros.Sum(r => (int?)r.Chamada.QuantidadeAulas) ?? 0,
                presencas.Sum(r => (int?)r.Chamada.QuantidadeAulas) ?? 0,
                infrequentes.Count(),
                semChamada.Count(),
                semChamada.Select(x => x.TurmaId).Distinct().Count()))
            .FirstOrDefaultAsync();

        return linha ?? new IndicadoresLinha(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    public async Task<IReadOnlyList<ChamadaRecenteLinha>> ListarUltimasChamadasAsync(int quantidade, int? professorId)
    {
        var chamadas = _context.Chamadas.AsNoTracking().AsQueryable();
        if (professorId is not null)
            chamadas = chamadas.Where(c => _context.ProfessorAlocacoes.Any(pa =>
                pa.ProfessorId == professorId && pa.Ativa && pa.TurmaId == c.TurmaId && pa.DisciplinaId == c.DisciplinaId));

        var linhas = await chamadas
            .OrderByDescending(c => c.Data).ThenByDescending(c => c.CreatedAt)
            .Take(quantidade)
            .Select(c => new
            {
                c.Id,
                Turma = c.Turma.NomeCompleto,
                Disciplina = c.Disciplina.Nome,
                c.RegistradoPorUsuarioId,
                Professor = _context.Professores
                    .Where(x => x.UsuarioId == c.RegistradoPorUsuarioId)
                    .Select(x => x.Nome)
                    .FirstOrDefault(),
                c.Data,
                Presentes = c.Registros.Count(r => r.Situacao == ChamadaAluno.SituacaoPresente),
                Alunos = c.Registros.Count(),
            })
            .ToListAsync();

        // Quem lançou sem ser professor (secretaria, gestão, Administrador global) aparece pelo e-mail.
        // O Administrador global não pertence à instituição: o filtro de tenant o esconderia.
        var semProfessor = linhas.Where(l => l.Professor is null).Select(l => l.RegistradoPorUsuarioId).Distinct().ToList();
        var emails = semProfessor.Count == 0
            ? new Dictionary<int, string>()
            : await _context.Users.IgnoreQueryFilters().AsNoTracking()
                .Where(u => semProfessor.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email);

        return linhas
            .Select(l => new ChamadaRecenteLinha(
                l.Id, l.Turma, l.Disciplina, l.Professor ?? emails.GetValueOrDefault(l.RegistradoPorUsuarioId),
                l.Data, l.Presentes, l.Alunos))
            .ToList();
    }

    public async Task<IReadOnlyList<MarcoCalendarioLinha>> ListarMarcosDoCalendarioAsync(DateOnly de, DateOnly ate)
    {
        // Períodos e anos letivos numa só consulta (UNION ALL).
        var periodos = _context.PeriodosAvaliativos.AsNoTracking()
            .Where(p => (p.DataInicio >= de && p.DataInicio <= ate) || (p.DataTermino >= de && p.DataTermino <= ate))
            .Select(p => new { p.AnoLetivo.AnoReferencia, PeriodoNome = (string?)p.Nome, p.DataInicio, p.DataTermino });

        var anos = _context.AnosLetivos.AsNoTracking()
            .Where(a => (a.DataInicio >= de && a.DataInicio <= ate) || (a.DataTermino >= de && a.DataTermino <= ate))
            .Select(a => new { a.AnoReferencia, PeriodoNome = (string?)null, a.DataInicio, a.DataTermino });

        return (await periodos.Concat(anos).ToListAsync())
            .Select(m => new MarcoCalendarioLinha(m.AnoReferencia, m.PeriodoNome, m.DataInicio, m.DataTermino))
            .ToList();
    }

    /// <summary>Alunos não inativos com enturmação vigente na data.</summary>
    private IQueryable<int> AlunosEnturmadosEm(DateOnly data)
        => _context.AlunosTurmas
            .Where(at => at.DataInicio <= data && (at.DataFim == null || at.DataFim >= data) &&
                         at.Aluno.Status != Aluno.StatusInativo)
            .Select(at => at.AlunoId)
            .Distinct();

    /// <summary>
    /// Pares turma/disciplina com professor alocado, em turma ativa de ano letivo vigente iniciado até
    /// "semChamadaDesde", sem nenhuma chamada a partir dessa data. Cada alocação ativa é um par distinto
    /// (índice único de turma/disciplina nas alocações ativas).
    /// </summary>
    private IQueryable<ProfessorAlocacao> AlocacoesSemChamada(DateOnly hoje, DateOnly semChamadaDesde, int? professorId)
    {
        var alocacoes = _context.ProfessorAlocacoes
            .Where(pa => pa.Ativa &&
                         pa.Turma.Status == Turma.StatusAtivo &&
                         pa.Turma.AnoLetivo.DataInicio <= semChamadaDesde &&
                         pa.Turma.AnoLetivo.DataTermino >= hoje);
        if (professorId is not null)
            alocacoes = alocacoes.Where(pa => pa.ProfessorId == professorId);

        return alocacoes
            .Where(pa => !_context.Chamadas.Any(c =>
                c.TurmaId == pa.TurmaId && c.DisciplinaId == pa.DisciplinaId && c.Data >= semChamadaDesde));
    }

    /// <summary>Registros de presença de chamadas no intervalo (do professor, quando informado).</summary>
    private IQueryable<ChamadaAluno> RegistrosDoPeriodo(DateOnly de, DateOnly ate, int? professorId)
    {
        var registros = _context.ChamadasAlunos
            .Where(r => r.Chamada.Data >= de && r.Chamada.Data <= ate);
        if (professorId is not null)
            registros = registros.Where(r => _context.ProfessorAlocacoes.Any(pa =>
                pa.ProfessorId == professorId && pa.Ativa &&
                pa.TurmaId == r.Chamada.TurmaId && pa.DisciplinaId == r.Chamada.DisciplinaId));
        return registros;
    }

}
