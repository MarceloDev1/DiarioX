using DiarioX.Server.Application.Relatorios;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Relatorios;

/// <summary>
/// Consultas dos relatórios. Os filtros globais do AppDbContext já restringem tudo à instituição e às
/// escolas do usuário; as agregações são projetadas para rodar no banco.
/// </summary>
public class RelatorioConsultas : IRelatorioConsultas
{
    private readonly AppDbContext _context;

    public RelatorioConsultas(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OpcoesRelatorioResponse> ObterOpcoesAsync()
    {
        var escolas = await _context.Escolas.AsNoTracking()
            .OrderBy(e => e.Nome)
            .Select(e => new OpcaoEscola(e.Id, e.Nome))
            .ToListAsync();

        var anos = await _context.AnosLetivos.AsNoTracking()
            .OrderByDescending(a => a.AnoReferencia)
            .Select(a => new OpcaoAnoLetivo(a.Id, a.AnoReferencia))
            .ToListAsync();

        var turmas = await _context.Turmas.AsNoTracking()
            .OrderBy(t => t.EtapaEnsino.OrdemCronologica).ThenBy(t => t.NomeCompleto)
            .Select(t => new OpcaoTurma(t.Id, t.NomeCompleto, t.EscolaId, t.AnoLetivoId, t.Turno, t.Status == Turma.StatusAtivo))
            .ToListAsync();

        return new OpcoesRelatorioResponse(escolas, anos, turmas);
    }

    public Task<string?> ObterNomeEscolaAsync(int escolaId)
        => _context.Escolas.AsNoTracking().Where(e => e.Id == escolaId).Select(e => e.Nome).FirstOrDefaultAsync();

    public Task<int?> ObterAnoReferenciaAsync(int anoLetivoId)
        => _context.AnosLetivos.AsNoTracking().Where(a => a.Id == anoLetivoId).Select(a => (int?)a.AnoReferencia).FirstOrDefaultAsync();

    public Task<TurmaDoRelatorio?> ObterTurmaAsync(int turmaId)
        => _context.Turmas.AsNoTracking()
            .Where(t => t.Id == turmaId)
            .Select(t => new TurmaDoRelatorio(t.Id, t.NomeCompleto, t.Escola.Nome, t.AnoLetivo.AnoReferencia, t.Turno))
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<OcupacaoTurmaLinha>> ListarOcupacaoDasTurmasAsync(int? anoLetivoId, int? escolaId, string? turno, DateOnly data)
    {
        var turmas = _context.Turmas.AsNoTracking().Where(t => t.Status == Turma.StatusAtivo);
        if (anoLetivoId is not null)
            turmas = turmas.Where(t => t.AnoLetivoId == anoLetivoId);
        if (escolaId is not null)
            turmas = turmas.Where(t => t.EscolaId == escolaId);
        if (turno is not null)
            turmas = turmas.Where(t => t.Turno == turno);

        return await turmas
            .OrderBy(t => t.Escola.Nome).ThenBy(t => t.EtapaEnsino.OrdemCronologica).ThenBy(t => t.NomeCompleto)
            .Select(t => new OcupacaoTurmaLinha(
                t.Id,
                t.NomeCompleto,
                t.Escola.Nome,
                t.EtapaEnsino.Nome,
                t.Turno,
                t.VagasOfertadas,
                // Mesma regra do faturamento e da chamada: aluno inativo não conta como enturmado.
                _context.AlunosTurmas.Count(at =>
                    at.TurmaId == t.Id &&
                    at.DataInicio <= data && (at.DataFim == null || at.DataFim >= data) &&
                    at.Aluno.Status != Aluno.StatusInativo)))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AlunoDaTurmaLinha>> ListarAlunosDaTurmaAsync(int turmaId, DateOnly data)
    {
        return await _context.AlunosTurmas.AsNoTracking()
            .Where(at => at.TurmaId == turmaId &&
                         at.DataInicio <= data && (at.DataFim == null || at.DataFim >= data) &&
                         at.Aluno.Status != Aluno.StatusInativo)
            .OrderBy(at => at.Aluno.Nome)
            .Select(at => new AlunoDaTurmaLinha(
                at.Aluno.Matricula,
                at.Aluno.Nome,
                at.Aluno.DataNascimento,
                at.Aluno.Sexo,
                at.Aluno.NecessidadeEspecial,
                at.Aluno.ResponsavelNome1,
                at.Aluno.ResponsavelTelefone1,
                at.DataInicio))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AlunoAguardandoLinha>> ListarAlunosAguardandoEnturmacaoAsync(int? escolaId)
    {
        var alunos = _context.Alunos.AsNoTracking().Where(a => a.Status == Aluno.StatusAtivoAguardandoEnturmacao);
        if (escolaId is not null)
            alunos = alunos.Where(a => a.EscolaId == escolaId);

        return await alunos
            .Select(a => new AlunoAguardandoLinha(a.Matricula, a.Nome, a.Escola.Nome, a.DataNascimento, a.CreatedAt))
            .ToListAsync();
    }
}
