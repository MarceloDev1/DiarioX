using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using DiarioX.Server.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Tests.Infrastructure.Data;

/// <summary>
/// Exercita o AlunoService com repositórios reais para cobrir o rastreamento de entidades do EF,
/// que os testes com mocks não alcançam.
/// </summary>
public class AlunoStatusPersistenceTests
{
    private const int TenantId = 1;

    [Fact]
    public async Task UpdateStatusAsync_ReativandoAlunoEnturmado_SalvaComoAtivo()
    {
        var database = Guid.NewGuid().ToString();
        var alunoId = Seed(database, enturmado: true);

        await using var context = CreateContext(database);
        var service = CreateService(context);

        var result = await service.UpdateStatusAsync(alunoId, new AlunoStatusRequest { Status = Aluno.StatusAtivo });

        Assert.True(result.Success);
        await using var verificacao = CreateContext(database);
        Assert.Equal(Aluno.StatusAtivo, verificacao.Alunos.Single(a => a.Id == alunoId).Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_ReativandoAlunoSemTurma_SalvaComoAguardandoEnturmacao()
    {
        var database = Guid.NewGuid().ToString();
        var alunoId = Seed(database, enturmado: false);

        await using var context = CreateContext(database);
        var service = CreateService(context);

        var result = await service.UpdateStatusAsync(alunoId, new AlunoStatusRequest { Status = Aluno.StatusAtivo });

        Assert.True(result.Success);
        await using var verificacao = CreateContext(database);
        Assert.Equal(Aluno.StatusAtivoAguardandoEnturmacao, verificacao.Alunos.Single(a => a.Id == alunoId).Status);
    }

    private static int Seed(string database, bool enturmado)
    {
        using var context = CreateContext(database);

        context.Tenants.Add(new Tenant { Id = TenantId, Nome = "Instituição", Slug = "inst" });
        var escola = new Escola { Nome = "Escola A", Cnpj = "123", Status = Escola.StatusAtivo };
        var anoLetivo = new AnoLetivo { AnoReferencia = 2026, DataInicio = new DateOnly(2026, 2, 1), DataTermino = new DateOnly(2026, 12, 15) };
        var aluno = new Aluno { Matricula = "20260001", Nome = "Aluno Teste", Escola = escola, Status = Aluno.StatusInativo };
        context.AddRange(escola, anoLetivo, aluno);

        if (enturmado)
        {
            var turma = new Turma { AnoLetivo = anoLetivo, Escola = escola, NomeCompleto = "6º Ano A", VagasOfertadas = 30 };
            context.Add(new AlunoTurma { Aluno = aluno, Turma = turma, DataInicio = new DateOnly(2026, 2, 1) });
        }

        context.SaveChanges();
        return aluno.Id;
    }

    private static AlunoService CreateService(AppDbContext context)
        => new(new AlunoRepository(context), new EscolaRepository(context), new AlunoTurmaRepository(context));

    private static AppDbContext CreateContext(string database)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(database)
            .Options;

        return new AppDbContext(options, new FixedTenantContext(TenantId));
    }

    private sealed class FixedTenantContext(int? tenantId) : ITenantContext
    {
        public int? TenantId { get; } = tenantId;
        public string? TenantSlug => TenantId?.ToString();
    }
}
