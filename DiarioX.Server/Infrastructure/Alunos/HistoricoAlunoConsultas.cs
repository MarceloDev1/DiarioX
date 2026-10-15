using DiarioX.Server.Application.Alunos;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Alunos;

/// <summary>
/// O histórico atravessa escolas: o filtro de escola é ignorado nas consultas e o acesso é liberado se o
/// aluno passou por alguma escola do escopo do usuário.
/// </summary>
public class HistoricoAlunoConsultas : IHistoricoAlunoConsultas
{
    private readonly AppDbContext _context;

    public HistoricoAlunoConsultas(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HistoricoAlunoDados?> ObterAsync(int alunoId)
    {
        var aluno = await _context.Alunos.IgnoreQueryFilters([AppDbContext.FiltroEscola]).AsNoTracking()
            .Where(a => a.Id == alunoId)
            .Select(a => new { a.Id, a.Nome, a.Matricula, a.CreatedAt, a.EscolaId, Escola = a.Escola.Nome, a.Status })
            .FirstOrDefaultAsync();
        if (aluno is null)
            return null;

        var vinculos = await _context.AlunosTurmas.IgnoreQueryFilters([AppDbContext.FiltroEscola]).AsNoTracking()
            .Where(v => v.AlunoId == alunoId)
            .OrderBy(v => v.DataInicio).ThenBy(v => v.Id)
            .Select(v => new VinculoHistoricoLinha(
                v.Id, v.Turma.NomeCompleto, v.Turma.EscolaId, v.Turma.Escola.Nome,
                v.DataInicio, v.DataFim, v.MotivoDesenturmacao, v.ObservacaoDesenturmacao))
            .ToListAsync();

        var transferencias = await _context.Transferencias.IgnoreQueryFilters([AppDbContext.FiltroEscola]).AsNoTracking()
            .Where(t => t.AlunoId == alunoId)
            .OrderBy(t => t.DataTransferencia).ThenBy(t => t.Id)
            .Select(t => new TransferenciaHistoricoLinha(
                t.Id, t.DataTransferencia, t.Tipo, t.EscolaOrigemId, t.EscolaOrigem.Nome,
                t.EscolaDestinoId, t.EscolaDestino, t.Turma == null ? null : t.Turma.NomeCompleto, t.Motivo))
            .ToListAsync();

        if (_context.EscopoPorEscola)
        {
            var permitidas = _context.EscolasPermitidas;
            var passouPeloEscopo = permitidas.Contains(aluno.EscolaId) ||
                                   vinculos.Any(v => permitidas.Contains(v.EscolaId)) ||
                                   transferencias.Any(t => permitidas.Contains(t.EscolaOrigemId));
            if (!passouPeloEscopo)
                return null;
        }

        return new HistoricoAlunoDados(aluno.Id, aluno.Nome, aluno.Matricula, aluno.CreatedAt, aluno.EscolaId,
            aluno.Escola, aluno.Status, vinculos, transferencias);
    }
}
