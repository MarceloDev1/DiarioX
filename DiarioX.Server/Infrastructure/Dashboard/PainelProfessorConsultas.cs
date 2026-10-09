using DiarioX.Server.Application.Dashboard;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Dashboard;

public class PainelProfessorConsultas : IPainelProfessorConsultas
{
    private readonly AppDbContext _context;

    public PainelProfessorConsultas(AppDbContext context)
    {
        _context = context;
    }

    public Task<ProfessorPainelLinha?> ObterProfessorDoUsuarioAsync(int usuarioId)
        => _context.ProfessoresDoUsuario(usuarioId)
            .Select(p => new ProfessorPainelLinha(p.Id, p.Nome))
            .FirstOrDefaultAsync();

    public Task<bool> UsuarioTemPerfilProfessorAsync(int usuarioId)
        => _context.UsuariosPerfis.AsNoTracking()
            .AnyAsync(up => up.UsuarioId == usuarioId && up.Perfil.Nome.ToLower() == Perfil.Professor.ToLower());

    public Task<AnoLetivo?> ObterAnoLetivoAsync(int anoReferencia)
        => _context.AnosLetivos.AsNoTracking()
            .Include(a => a.Periodos)
            .FirstOrDefaultAsync(a => a.AnoReferencia == anoReferencia);

    public async Task<IReadOnlyList<DiarioLinha>> ListarDiariosAsync(int professorId, int anoLetivoId)
        => await _context.ProfessorAlocacoes.AsNoTracking()
            .Where(pa => pa.ProfessorId == professorId && pa.Ativa &&
                         pa.Turma.AnoLetivoId == anoLetivoId && pa.Turma.Status == Turma.StatusAtivo)
            .Select(pa => new DiarioLinha(
                pa.TurmaId, pa.Turma.NomeCompleto, pa.Turma.EscolaId, pa.Turma.Escola.Nome, pa.Turma.EtapaEnsino.Nome,
                pa.Turma.EtapaEnsino.TipoFrequencia, pa.DisciplinaId, pa.Disciplina.Nome))
            .ToListAsync();

    public async Task<IReadOnlyList<HorarioAula>> ListarGradeAsync(IReadOnlyCollection<int> turmaIds)
        => await _context.HorariosAula.AsNoTracking()
            .Where(h => turmaIds.Contains(h.TurmaId))
            .ToListAsync();

    public async Task<IReadOnlyList<CalendarioLetivo>> ListarCalendariosPublicadosAsync(int anoLetivoId)
        => await _context.CalendariosLetivos.AsNoTracking()
            .Include(c => c.Eventos)
            .Where(c => c.AnoLetivoId == anoLetivoId && c.PublicadoEm != null)
            .ToListAsync();

    public async Task<IReadOnlyList<RegistroDiarioLinha>> ListarChamadasAsync(IReadOnlyCollection<int> turmaIds, DateOnly de, DateOnly ate)
        => await _context.Chamadas.AsNoTracking()
            .Where(c => turmaIds.Contains(c.TurmaId) && c.Data >= de && c.Data <= ate)
            .Select(c => new RegistroDiarioLinha(c.TurmaId, c.DisciplinaId, c.Data))
            .ToListAsync();

    public async Task<IReadOnlyList<RegistroDiarioLinha>> ListarConteudosAsync(IReadOnlyCollection<int> turmaIds, DateOnly de, DateOnly ate)
        => await _context.ConteudosMinistrados.AsNoTracking()
            .Where(c => turmaIds.Contains(c.TurmaId) && c.Data >= de && c.Data <= ate)
            .Select(c => new RegistroDiarioLinha(c.TurmaId, c.DisciplinaId, c.Data))
            .ToListAsync();

    public async Task<IReadOnlyList<AvaliacaoPainelLinha>> ListarAvaliacoesAsync(
        IReadOnlyCollection<int> turmaIds, IReadOnlyCollection<int> periodoIds)
    {
        var linhas = await _context.Avaliacoes.AsNoTracking()
            .Where(a => turmaIds.Contains(a.TurmaId) && periodoIds.Contains(a.PeriodoAvaliativoId))
            .Select(a => new
            {
                a.Id, a.TurmaId, a.DisciplinaId, a.PeriodoAvaliativoId, a.Data, a.Tipo,
                Alunos = a.Notas.Select(n => n.AlunoId).ToList(),
            })
            .ToListAsync();

        return linhas
            .Select(a => new AvaliacaoPainelLinha(
                a.Id, a.TurmaId, a.DisciplinaId, a.PeriodoAvaliativoId, a.Data, a.Tipo == Avaliacao.TipoRecuperacao, a.Alunos))
            .ToList();
    }

    public async Task<IReadOnlyList<EnturmacaoLinha>> ListarEnturmacoesAsync(IReadOnlyCollection<int> turmaIds, DateOnly de, DateOnly ate)
        => await _context.AlunosTurmas.AsNoTracking()
            // DataFim < DataInicio é vínculo desfeito no mesmo dia: não vale para nenhuma data.
            .Where(at => turmaIds.Contains(at.TurmaId) && at.DataInicio <= ate &&
                         (at.DataFim == null || (at.DataFim >= de && at.DataFim >= at.DataInicio)))
            .Select(at => new EnturmacaoLinha(at.TurmaId, at.AlunoId, at.DataInicio, at.DataFim))
            .ToListAsync();
}
