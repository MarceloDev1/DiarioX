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

    [Fact]
    public async Task DesenturmarAsync_EncerraVinculoGravaMotivoELiberaAVaga()
    {
        var database = Guid.NewGuid().ToString();
        var turmaId = Seed(database, vagas: 2,
            (new DateOnly(2026, 2, 1), null),
            (new DateOnly(2026, 2, 1), null));
        var hoje = new DateOnly(2026, 9, 30);

        int desenturmadoId, outroId;
        await using (var context = CreateContext(database))
        {
            var repository = new AlunoTurmaRepository(context);
            var vinculos = await repository.GetAtivasByTurmaIdAsync(turmaId);
            (desenturmadoId, outroId) = (vinculos[0].AlunoId, vinculos[1].AlunoId);
            Assert.False(await repository.HasVacancyAsync(turmaId, hoje));

            await repository.DesenturmarAsync(turmaId, new Dictionary<int, string> { [desenturmadoId] = Aluno.StatusNaoCompareceu },
                hoje, AlunoTurma.MotivoNaoCompareceu, "Não compareceu desde a matrícula");
        }

        await using var verificacao = CreateContext(database);
        var vinculo = verificacao.AlunosTurmas.Single(x => x.AlunoId == desenturmadoId);
        Assert.Equal(hoje.AddDays(-1), vinculo.DataFim);
        Assert.Equal(AlunoTurma.MotivoNaoCompareceu, vinculo.MotivoDesenturmacao);
        Assert.Equal("Não compareceu desde a matrícula", vinculo.ObservacaoDesenturmacao);
        Assert.Equal(Aluno.StatusNaoCompareceu, verificacao.Alunos.Single(a => a.Id == desenturmadoId).Status);

        Assert.Null(verificacao.AlunosTurmas.Single(x => x.AlunoId == outroId).DataFim);
        Assert.Equal(Aluno.StatusAtivo, verificacao.Alunos.Single(a => a.Id == outroId).Status);

        var repositorio = new AlunoTurmaRepository(verificacao);
        Assert.True(await repositorio.HasVacancyAsync(turmaId, hoje));
        Assert.Single(await repositorio.GetAtivasByTurmaIdAsync(turmaId));
    }

    [Fact]
    public async Task DesenturmarAsync_VinculoIniciadoNoMesmoDia_NaoValeParaNenhumaData()
    {
        var hoje = new DateOnly(2026, 9, 30);
        var database = Guid.NewGuid().ToString();
        var turmaId = Seed(database, vagas: 1, (hoje, null));

        await using (var context = CreateContext(database))
        {
            var repository = new AlunoTurmaRepository(context);
            var alunoId = (await repository.GetAtivasByTurmaIdAsync(turmaId)).Single().AlunoId;

            await repository.DesenturmarAsync(turmaId, new Dictionary<int, string> { [alunoId] = Aluno.StatusAtivoAguardandoEnturmacao },
                hoje, AlunoTurma.MotivoErroMatricula, null);
        }

        await using var verificacao = CreateContext(database);
        var vinculo = verificacao.AlunosTurmas.Single();
        Assert.Equal(hoje.AddDays(-1), vinculo.DataFim);
        Assert.Equal(AlunoTurma.MotivoErroMatricula, vinculo.MotivoDesenturmacao);
        Assert.Equal(0, await new AlunoTurmaRepository(verificacao).GetOcupacaoMaximaAsync(turmaId, hoje));
    }

    [Fact]
    public async Task DesenturmarAsync_AlunoForaDaTurma_NaoGravaNada()
    {
        var database = Guid.NewGuid().ToString();
        var turmaId = Seed(database, vagas: 2, (new DateOnly(2026, 2, 1), null));
        var foraDaTurma = SeedAlunosAguardando(database, 1).Single();

        await using (var context = CreateContext(database))
        {
            var repository = new AlunoTurmaRepository(context);
            var enturmadoId = (await repository.GetAtivasByTurmaIdAsync(turmaId)).Single().AlunoId;

            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DesenturmarAsync(turmaId,
                new Dictionary<int, string>
                {
                    [enturmadoId] = Aluno.StatusAtivoAguardandoEnturmacao,
                    [foraDaTurma] = Aluno.StatusAtivoAguardandoEnturmacao,
                },
                new DateOnly(2026, 9, 30), AlunoTurma.MotivoReestruturacaoInterna, null));
        }

        await using var verificacao = CreateContext(database);
        Assert.Null(verificacao.AlunosTurmas.Single().DataFim);
        Assert.Equal(Aluno.StatusAtivo, verificacao.Alunos.Single(a => a.Id != foraDaTurma).Status);
    }

    [Fact]
    public async Task GetAtivasAsync_ListaSoAsAtivasDasEscolasDoEscopo()
    {
        var database = Guid.NewGuid().ToString();
        int norteId;
        using (var context = CreateContext(database))
        {
            context.Tenants.Add(new Tenant { Id = TenantId, Nome = "Instituição", Slug = "inst" });
            var norte = new Escola { Nome = "Escola Norte", Cnpj = "1", Status = Escola.StatusAtivo };
            var sul = new Escola { Nome = "Escola Sul", Cnpj = "2", Status = Escola.StatusAtivo };
            var ano = new AnoLetivo { AnoReferencia = 2026, DataInicio = new DateOnly(2026, 2, 1), DataTermino = new DateOnly(2026, 12, 15) };
            var modalidade = new ModalidadeEnsino { Nome = "Fundamental", Sigla = "EF" };
            var etapa = new EtapaEnsino { ModalidadeEnsino = modalidade, Nome = "1º Ano", Sigla = "1A" };
            Turma NovaTurma(Escola escola) => new()
            {
                AnoLetivo = ano, Escola = escola, ModalidadeEnsino = modalidade, EtapaEnsino = etapa,
                NomeIdentificador = "A", NomeCompleto = $"1º Ano A {escola.Nome}", VagasOfertadas = 10,
            };
            var turmaNorte = NovaTurma(norte);
            var turmaSul = NovaTurma(sul);

            // Na Escola Norte: um ativo (cadastrado na Escola Sul) e um já desenturmado.
            context.AddRange(
                new AlunoTurma { Aluno = new Aluno { Matricula = "1", Nome = "Ativo Norte", Escola = sul }, Turma = turmaNorte, DataInicio = new DateOnly(2026, 2, 1) },
                new AlunoTurma { Aluno = new Aluno { Matricula = "2", Nome = "Saiu Norte", Escola = norte }, Turma = turmaNorte, DataInicio = new DateOnly(2026, 2, 1), DataFim = new DateOnly(2026, 3, 1) },
                new AlunoTurma { Aluno = new Aluno { Matricula = "3", Nome = "Ativo Sul", Escola = sul }, Turma = turmaSul, DataInicio = new DateOnly(2026, 2, 1) });
            context.SaveChanges();
            norteId = norte.Id;
        }

        await using var escopoNorte = CreateContext(database, escolaIds: [norteId]);
        var ativas = await new AlunoTurmaRepository(escopoNorte).GetAtivasAsync();

        var vinculo = Assert.Single(ativas);
        Assert.Equal("Ativo Norte", vinculo.Aluno.Nome);
        Assert.Equal("Escola Norte", vinculo.Turma.Escola.Nome);
        Assert.Equal("Fundamental", vinculo.Turma.ModalidadeEnsino.Nome);
        Assert.Equal("1º Ano", vinculo.Turma.EtapaEnsino.Nome);
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

    private static AppDbContext CreateContext(string database, IReadOnlyList<int>? escolaIds = null)
    {
        // O banco em memória não tem transações; aqui só interessa o efeito final da gravação.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(database)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, new FixedTenantContext(TenantId, escolaIds));
    }

    private sealed class FixedTenantContext(int? tenantId, IReadOnlyList<int>? escolaIds) : ITenantContext
    {
        public int? TenantId { get; } = tenantId;
        public string? TenantSlug => TenantId?.ToString();
        public IReadOnlyList<int>? EscolaIds { get; } = escolaIds;
    }
}
