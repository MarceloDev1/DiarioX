using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using DiarioX.Server.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DiarioX.Server.Tests.Infrastructure.Data;

public class AlunoTurmaRepositoryTests
{
    private const int TenantId = 1;

    [Fact]
    public async Task GetOcupacaoMaximaAsync_DataRetroativa_ConsideraAlunosQueEntraramDepois()
    {
        // Em 15/03 só há 1 aluno na turma, mas a partir de 01/09 ela tem 2 (a lotação).
        var database = Guid.NewGuid().ToString();
        var turmaId = Seed(database, vagas: 2,
            (new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 31)),
            (new DateOnly(2026, 9, 1), null),
            (new DateOnly(2026, 9, 1), null));

        await using var context = CreateContext(database);
        var repository = new AlunoTurmaRepository(context);

        Assert.Equal(2, await repository.GetOcupacaoMaximaAsync(turmaId, new DateOnly(2026, 3, 15)));
        Assert.False(await repository.HasVacancyAsync(turmaId, new DateOnly(2026, 3, 15)));
    }

    [Fact]
    public async Task GetOcupacaoMaximaAsync_IgnoraVinculosEncerradosAntesDaData()
    {
        var database = Guid.NewGuid().ToString();
        var turmaId = Seed(database, vagas: 2,
            (new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 31)),
            (new DateOnly(2026, 2, 1), null));

        await using var context = CreateContext(database);
        var repository = new AlunoTurmaRepository(context);

        Assert.Equal(1, await repository.GetOcupacaoMaximaAsync(turmaId, new DateOnly(2026, 4, 1)));
        Assert.True(await repository.HasVacancyAsync(turmaId, new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public async Task EnturmarAsync_Lote_GravaVinculosEAtualizaStatus()
    {
        var database = Guid.NewGuid().ToString();
        var turmaId = Seed(database, vagas: 3);
        var alunoIds = SeedAlunosAguardando(database, 3);

        await using (var context = CreateContext(database))
        {
            var gravou = await new AlunoTurmaRepository(context).EnturmarAsync(alunoIds, turmaId, new DateOnly(2026, 2, 1));
            Assert.True(gravou);
        }

        await using var verificacao = CreateContext(database);
        Assert.Equal(3, verificacao.AlunosTurmas.Count(x => x.TurmaId == turmaId && x.DataFim == null));
        Assert.All(verificacao.Alunos.Where(a => alunoIds.Contains(a.Id)), a => Assert.Equal(Aluno.StatusAtivo, a.Status));
    }

    [Fact]
    public async Task EnturmarAsync_LoteMaiorQueAsVagas_NaoGravaNenhumVinculo()
    {
        var database = Guid.NewGuid().ToString();
        var turmaId = Seed(database, vagas: 2);
        var alunoIds = SeedAlunosAguardando(database, 3);

        await using (var context = CreateContext(database))
        {
            var gravou = await new AlunoTurmaRepository(context).EnturmarAsync(alunoIds, turmaId, new DateOnly(2026, 2, 1));
            Assert.False(gravou);
        }

        await using var verificacao = CreateContext(database);
        Assert.Empty(verificacao.AlunosTurmas);
        Assert.All(verificacao.Alunos, a => Assert.Equal(Aluno.StatusAtivoAguardandoEnturmacao, a.Status));
    }

    private static int Seed(string database, int vagas, params (DateOnly Inicio, DateOnly? Fim)[] vinculos)
    {
        using var context = CreateContext(database);

        context.Tenants.Add(new Tenant { Id = TenantId, Nome = "Instituição", Slug = "inst" });
        var escola = new Escola { Nome = "Escola A", Cnpj = "123", Status = Escola.StatusAtivo };
        var anoLetivo = new AnoLetivo { AnoReferencia = 2026, DataInicio = new DateOnly(2026, 2, 1), DataTermino = new DateOnly(2026, 12, 15) };
        var turma = new Turma { AnoLetivo = anoLetivo, Escola = escola, NomeCompleto = "6º Ano A", VagasOfertadas = vagas };
        context.AddRange(escola, anoLetivo, turma);

        for (var i = 0; i < vinculos.Length; i++)
        {
            var aluno = new Aluno { Matricula = $"2026{i:0000}", Nome = $"Enturmado {i}", Escola = escola, Status = Aluno.StatusAtivo };
            context.Add(new AlunoTurma { Aluno = aluno, Turma = turma, DataInicio = vinculos[i].Inicio, DataFim = vinculos[i].Fim });
        }

        context.SaveChanges();
        return turma.Id;
    }

    private static List<int> SeedAlunosAguardando(string database, int quantidade)
    {
        using var context = CreateContext(database);
        var escola = context.Escolas.Single();
        var alunos = Enumerable.Range(1, quantidade)
            .Select(i => new Aluno { Matricula = $"2026A{i:000}", Nome = $"Aguardando {i}", Escola = escola, Status = Aluno.StatusAtivoAguardandoEnturmacao })
            .ToList();

        context.AddRange(alunos);
        context.SaveChanges();
        return alunos.Select(a => a.Id).ToList();
    }

    private static AppDbContext CreateContext(string database)
    {
        // O banco em memória não tem transações; aqui só interessa o efeito final da gravação.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(database)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, new FixedTenantContext(TenantId));
    }

    private sealed class FixedTenantContext(int? tenantId) : ITenantContext
    {
        public int? TenantId { get; } = tenantId;
        public string? TenantSlug => TenantId?.ToString();
    }
}
