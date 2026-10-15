using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Alunos;
using DiarioX.Server.Infrastructure.Data;
using DiarioX.Server.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DiarioX.Server.Tests.Infrastructure.Data;

public class TransferenciaRepositoryTests
{
    private const int TenantId = 1;
    private static readonly DateOnly DataTransferencia = new(2026, 9, 30);

    [Fact]
    public async Task TransferirAsync_EncerraEnturmacaoMudaStatusELiberaAVaga()
    {
        var database = Guid.NewGuid().ToString();
        var (alunoId, turmaId, vinculoId) = Seed(database, enturmado: true);

        await using (var context = CreateContext(database))
            await new TransferenciaRepository(context).TransferirAsync(NovaTransferencia(database, alunoId, turmaId), vinculoId);

        await using var verificacao = CreateContext(database);
        var vinculo = verificacao.AlunosTurmas.Single();
        Assert.Equal(DataTransferencia.AddDays(-1), vinculo.DataFim);
        Assert.Equal(AlunoTurma.MotivoTransferencia, vinculo.MotivoDesenturmacao);
        Assert.Equal(Aluno.StatusTransferido, verificacao.Alunos.Single().Status);
        Assert.Equal(0, await new AlunoTurmaRepository(verificacao).GetOcupacaoMaximaAsync(turmaId, DataTransferencia));

        var gravada = Assert.Single(await new TransferenciaRepository(verificacao).GetByAlunoIdAsync(alunoId));
        Assert.Equal("EE Destino", gravada.EscolaDestino);
        Assert.Equal("Escola A", gravada.EscolaOrigem.Nome);
        Assert.Equal("6º Ano A", gravada.Turma!.NomeCompleto);
        Assert.Equal(2026, gravada.AnoLetivo.AnoReferencia);
    }

    [Fact]
    public async Task TransferirAsync_AlunoJaTransferido_NaoGravaDeNovo()
    {
        var database = Guid.NewGuid().ToString();
        var (alunoId, _, _) = Seed(database, enturmado: false);

        await using (var context = CreateContext(database))
            await new TransferenciaRepository(context).TransferirAsync(NovaTransferencia(database, alunoId, null), null);

        await using (var context = CreateContext(database))
        {
            var erro = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new TransferenciaRepository(context).TransferirAsync(NovaTransferencia(database, alunoId, null), null));
            Assert.Equal("Este aluno já possui o status de Transferido no sistema.", erro.Message);
        }

        await using var verificacao = CreateContext(database);
        Assert.Single(verificacao.Transferencias);
    }

    [Fact]
    public async Task TransferirAsync_EnturmacaoDiferenteDaEsperada_NaoGravaNada()
    {
        var database = Guid.NewGuid().ToString();
        var (alunoId, turmaId, _) = Seed(database, enturmado: true);

        await using (var context = CreateContext(database))
        {
            // A tela foi preenchida quando o aluno ainda aguardava enturmação.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new TransferenciaRepository(context).TransferirAsync(NovaTransferencia(database, alunoId, turmaId), null));
        }

        await using var verificacao = CreateContext(database);
        Assert.Empty(verificacao.Transferencias);
        Assert.Null(verificacao.AlunosTurmas.Single().DataFim);
        Assert.Equal(Aluno.StatusAtivo, verificacao.Alunos.Single().Status);
    }

    [Fact]
    public async Task TransferirAsync_EntreEscolasDaRede_LevaOAlunoParaADestinoAguardandoEnturmacao()
    {
        var database = Guid.NewGuid().ToString();
        var (alunoId, turmaId, vinculoId) = Seed(database, enturmado: true);
        var (origemId, destinoId) = ComEscolaDestino(database);

        var transferencia = NovaTransferencia(database, alunoId, turmaId);
        transferencia.Tipo = Transferencia.TipoEntreEscolasDaRede;
        transferencia.EscolaDestinoId = destinoId;
        transferencia.EscolaDestino = "Escola B";
        await using (var context = CreateContext(database))
            await new TransferenciaRepository(context).TransferirAsync(transferencia, vinculoId);

        await using var verificacao = CreateContext(database);
        var aluno = verificacao.Alunos.Single();
        Assert.Equal(destinoId, aluno.EscolaId);
        Assert.Equal(Aluno.StatusAtivoAguardandoEnturmacao, aluno.Status);
        Assert.Equal(AlunoTurma.MotivoTransferencia, verificacao.AlunosTurmas.Single().MotivoDesenturmacao);

        // A escola de origem, mesmo com escopo só dela, enxerga o aluno que saiu.
        await using var origem = CreateContext(database, escolaIds: [origemId]);
        var alunos = new AlunoRepository(origem);
        Assert.Empty(await alunos.GetByEscolaAsync(origemId));
        var transferido = Assert.Single(await alunos.GetTransferidosDaEscolaAsync(origemId));
        Assert.Equal("Escola B", transferido.Escola.Nome);
        Assert.Empty(await alunos.GetTransferidosDaEscolaAsync(destinoId));
        Assert.Single(await new AlunoRepository(verificacao).GetByEscolaAsync(destinoId));
    }

    [Fact]
    public async Task HistoricoAlunoConsultas_LiberaQuemPassouPeloEscopoENegaOsDemais()
    {
        var database = Guid.NewGuid().ToString();
        var (alunoId, turmaId, vinculoId) = Seed(database, enturmado: true);
        var (origemId, destinoId) = ComEscolaDestino(database);
        int outraEscolaId;
        await using (var context = CreateContext(database))
        {
            var outra = new Escola { Nome = "Escola C", Cnpj = "3", Status = Escola.StatusAtivo };
            context.Add(outra);
            context.SaveChanges();
            outraEscolaId = outra.Id;
        }

        var transferencia = NovaTransferencia(database, alunoId, turmaId);
        transferencia.Tipo = Transferencia.TipoEntreEscolasDaRede;
        transferencia.EscolaDestinoId = destinoId;
        transferencia.EscolaDestino = "Escola B";
        await using (var context = CreateContext(database))
            await new TransferenciaRepository(context).TransferirAsync(transferencia, vinculoId);

        await using (var origem = CreateContext(database, escolaIds: [origemId]))
        {
            var dados = await new HistoricoAlunoConsultas(origem).ObterAsync(alunoId);
            Assert.NotNull(dados);
            Assert.Equal("Escola B", dados!.EscolaNome);
            Assert.Equal("6º Ano A", Assert.Single(dados.Vinculos).Turma);
            Assert.Equal(destinoId, Assert.Single(dados.Transferencias).EscolaDestinoId);
        }

        await using (var destino = CreateContext(database, escolaIds: [destinoId]))
            Assert.NotNull(await new HistoricoAlunoConsultas(destino).ObterAsync(alunoId));

        await using var outraEscola = CreateContext(database, escolaIds: [outraEscolaId]);
        Assert.Null(await new HistoricoAlunoConsultas(outraEscola).ObterAsync(alunoId));
    }

    private static (int OrigemId, int DestinoId) ComEscolaDestino(string database)
    {
        using var context = CreateContext(database);
        var destino = new Escola { Nome = "Escola B", Cnpj = "2", Status = Escola.StatusAtivo };
        context.Add(destino);
        context.SaveChanges();
        return (context.Escolas.Single(e => e.Nome == "Escola A").Id, destino.Id);
    }

    private static Transferencia NovaTransferencia(string database, int alunoId, int? turmaId)
    {
        using var context = CreateContext(database);
        return new Transferencia
        {
            AlunoId = alunoId,
            EscolaOrigemId = context.Escolas.Single(e => e.Nome == "Escola A").Id,
            TurmaId = turmaId,
            AnoLetivoId = context.AnosLetivos.Single().Id,
            DataTransferencia = DataTransferencia,
            Tipo = Transferencia.TipoOutraRede,
            EscolaDestino = "EE Destino",
            RegistradoPorUsuarioId = 1,
        };
    }

    private static (int AlunoId, int TurmaId, int? VinculoId) Seed(string database, bool enturmado)
    {
        using var context = CreateContext(database);
        context.Tenants.Add(new Tenant { Id = TenantId, Nome = "Instituição", Slug = "inst" });
        var escola = new Escola { Nome = "Escola A", Cnpj = "1", Status = Escola.StatusAtivo };
        var ano = new AnoLetivo { AnoReferencia = 2026, DataInicio = new DateOnly(2026, 2, 1), DataTermino = new DateOnly(2026, 12, 15) };
        var turma = new Turma { AnoLetivo = ano, Escola = escola, NomeCompleto = "6º Ano A", VagasOfertadas = 1 };
        var aluno = new Aluno
        {
            Matricula = "20260001", Nome = "Carla Mendes", Escola = escola,
            Status = enturmado ? Aluno.StatusAtivo : Aluno.StatusAtivoAguardandoEnturmacao,
        };
        context.AddRange(escola, ano, turma, aluno);

        AlunoTurma? vinculo = null;
        if (enturmado)
        {
            vinculo = new AlunoTurma { Aluno = aluno, Turma = turma, DataInicio = new DateOnly(2026, 2, 10) };
            context.Add(vinculo);
        }

        context.SaveChanges();
        return (aluno.Id, turma.Id, vinculo?.Id);
    }

    private static AppDbContext CreateContext(string database, IReadOnlyList<int>? escolaIds = null)
    {
        // O banco em memória não tem transações nem FOR UPDATE; aqui só interessa o efeito final da gravação.
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
