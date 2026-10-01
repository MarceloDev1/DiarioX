using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class RemanejamentoLoteServiceTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);
    private static readonly DateOnly InicioDoAno = Hoje.AddDays(-100);

    [Fact]
    public async Task RemanejarEmLoteAsync_SemAlunos_ReturnsValidation()
    {
        var f = new Fixture();

        var result = await f.Service.RemanejarEmLoteAsync(Request(alunoIds: []));

        Assert.Equal("Selecione ao menos um aluno para remanejar.", result.Message);
        f.VerifyNadaGravado();
    }

    [Fact]
    public async Task RemanejarEmLoteAsync_DestinoDeOutraEtapa_ReturnsValidation()
    {
        var f = new Fixture();
        f.Destino.EtapaEnsinoId = 99;

        var result = await f.Service.RemanejarEmLoteAsync(Request());

        Assert.Equal("A turma de destino deve ser uma turma ativa da mesma escola, do mesmo ano letivo e da mesma etapa da turma atual.", result.Message);
        f.VerifyNadaGravado();
    }

    [Fact]
    public async Task RemanejarEmLoteAsync_AlunosImpedidos_ListaFalhasENaoGravaNada()
    {
        var f = new Fixture();
        f.Vinculos[2].Aluno.Status = Aluno.StatusInativo;
        f.Vinculos[3].DataInicio = Hoje;

        var result = await f.Service.RemanejarEmLoteAsync(Request(alunoIds: [1, 2, 3, 9]));

        Assert.False(result.Success);
        Assert.Equal("3 alunos não podem ser remanejados. Nenhum remanejamento foi realizado.", result.Message);
        Assert.Equal(
        [
            new RemanejamentoFalha(2, "Aluno inativo."),
            new RemanejamentoFalha(3, $"Enturmado(a) em {Hoje:dd/MM/yyyy}; a movimentação deve ser posterior a essa data."),
            new RemanejamentoFalha(9, "Aluno não está enturmado nesta turma."),
        ], result.Falhas);
        f.VerifyNadaGravado();
    }

    [Fact]
    public async Task RemanejarEmLoteAsync_SelecionadosExcedemVagas_ReturnsConflict()
    {
        var f = new Fixture();
        f.AlunoTurmas.Setup(r => r.GetOcupacaoMaximaAsync(f.Destino.Id, Hoje)).ReturnsAsync(f.Destino.VagasOfertadas - 2);

        var result = await f.Service.RemanejarEmLoteAsync(Request(alunoIds: [1, 2, 3]));

        Assert.Equal(AlunoResultError.Conflict, result.Error);
        Assert.Equal("A turma de destino possui 2 vagas disponíveis, mas 3 alunos foram selecionados.", result.Message);
        f.VerifyNadaGravado();
    }

    [Fact]
    public async Task RemanejarEmLoteAsync_QuandoValido_RemanejaTodosDeUmaVez()
    {
        var f = new Fixture();
        f.AlunoTurmas.Setup(r => r.RemanejarAsync(f.Origem.Id, It.IsAny<IReadOnlyCollection<int>>(), f.Destino.Id, Hoje, null))
            .ReturnsAsync(true);

        var result = await f.Service.RemanejarEmLoteAsync(Request(alunoIds: [1, 2, 2], motivo: "  "));

        Assert.True(result.Success, result.Message);
        Assert.Equal("2 alunos remanejados com sucesso para a turma 1º Ano B!", result.Message);
        f.AlunoTurmas.Verify(r => r.RemanejarAsync(f.Origem.Id,
            It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 1, 2 })), f.Destino.Id, Hoje, null), Times.Once);
    }

    [Fact]
    public async Task GetDestinosRemanejamentoAsync_SoTurmasCompativeisComVaga()
    {
        var f = new Fixture();
        var cheia = Turma(4, "1º Ano C");
        var outraEtapa = Turma(5, "2º Ano A");
        outraEtapa.EtapaEnsinoId = 99;
        var inativa = Turma(6, "1º Ano D");
        inativa.Status = DiarioX.Server.Domain.Entities.Turma.StatusInativo;
        f.Turmas.Setup(r => r.GetAllAsync()).ReturnsAsync([f.Origem, f.Destino, cheia, outraEtapa, inativa]);
        f.AlunoTurmas.Setup(r => r.GetOcupacaoMaximaAsync(cheia.Id, Hoje)).ReturnsAsync(cheia.VagasOfertadas);
        f.AlunoTurmas.Setup(r => r.GetOcupacaoMaximaAsync(f.Destino.Id, Hoje)).ReturnsAsync(28);

        var destinos = await f.Service.GetDestinosRemanejamentoAsync(f.Origem.Id, Hoje);

        var destino = Assert.Single(destinos!);
        Assert.Equal((f.Destino.Id, 2), (destino.TurmaId, destino.VagasDisponiveis));
    }

    private static RemanejamentoLoteRequest Request(List<int>? alunoIds = null, string? motivo = null) => new()
    {
        TurmaOrigemId = 1,
        TurmaDestinoId = 2,
        DataMovimentacao = Hoje,
        AlunoIds = alunoIds ?? [1],
        Motivo = motivo,
    };

    private static Turma Turma(int id, string nome) => new()
    {
        Id = id,
        EscolaId = 1,
        AnoLetivoId = 1,
        EtapaEnsinoId = 26,
        NomeCompleto = nome,
        Status = DiarioX.Server.Domain.Entities.Turma.StatusAtivo,
        VagasOfertadas = 30,
        AnoLetivo = new AnoLetivo { Id = 1, AnoReferencia = Hoje.Year, DataInicio = InicioDoAno, DataTermino = Hoje.AddDays(100) },
    };

    /// <summary>Turma de origem "1º Ano A" com os alunos 1, 2 e 3; destino "1º Ano B" (mesma escola, ano e etapa).</summary>
    private sealed class Fixture
    {
        public Mock<IAlunoTurmaRepository> AlunoTurmas { get; } = new();
        public Mock<ITurmaRepository> Turmas { get; } = new();
        public Turma Origem { get; } = Turma(1, "1º Ano A");
        public Turma Destino { get; } = Turma(2, "1º Ano B");
        public Dictionary<int, AlunoTurma> Vinculos { get; }
        public RemanejamentoAlunoService Service { get; }

        public Fixture()
        {
            Vinculos = new[] { 1, 2, 3 }.ToDictionary(id => id, id => new AlunoTurma
            {
                Id = 100 + id, AlunoId = id, TurmaId = Origem.Id, DataInicio = InicioDoAno,
                Aluno = new Aluno { Id = id, Nome = $"Aluno {id}", Status = Aluno.StatusAtivo },
            });

            Turmas.Setup(r => r.GetByIdAsync(Origem.Id)).ReturnsAsync(Origem);
            Turmas.Setup(r => r.GetByIdAsync(Destino.Id)).ReturnsAsync(Destino);
            AlunoTurmas.Setup(r => r.GetAtivasByTurmaIdAsync(Origem.Id)).ReturnsAsync(() => Vinculos.Values.ToList());

            Service = new RemanejamentoAlunoService(new Mock<IAlunoRepository>().Object, AlunoTurmas.Object, Turmas.Object);
        }

        public void VerifyNadaGravado()
            => AlunoTurmas.Verify(r => r.RemanejarAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<int>(),
                It.IsAny<DateOnly>(), It.IsAny<string?>()), Times.Never);
    }
}
