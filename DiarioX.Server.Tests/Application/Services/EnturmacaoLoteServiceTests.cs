using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class EnturmacaoLoteServiceTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public async Task EnturmarEmLoteAsync_SemAlunos_ReturnsValidation()
    {
        var (service, _, alunoTurmaRepository) = BuildService();

        var result = await service.EnturmarEmLoteAsync(new EnturmacaoLoteRequest { TurmaId = 1, DataInicio = Hoje });

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("Selecione ao menos um aluno para enturmar.", result.Message);
        VerifyNothingPersisted(alunoTurmaRepository);
    }

    [Fact]
    public async Task EnturmarEmLoteAsync_ComAlunosImpedidos_ListaFalhasENaoGravaNada()
    {
        var (service, alunoRepository, alunoTurmaRepository) = BuildService();
        alunoRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync(
        [
            BuildAluno(1),
            BuildAluno(2, escolaId: 99),
            BuildAluno(3, status: Aluno.StatusInativo),
            BuildAluno(5),
        ]);
        alunoTurmaRepository.Setup(x => x.GetAlunoIdsComEnturmacaoAtivaAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync([5]);

        var result = await service.EnturmarEmLoteAsync(new EnturmacaoLoteRequest
        {
            TurmaId = 1,
            DataInicio = Hoje,
            AlunoIds = [1, 2, 3, 4, 5],
        });

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("4 alunos não podem ser enturmados nesta turma. Nenhuma enturmação foi realizada.", result.Message);
        Assert.Equal(
        [
            new EnturmacaoLoteFalha(2, "Aluno pertence a outra escola."),
            new EnturmacaoLoteFalha(3, "Aluno inativo."),
            new EnturmacaoLoteFalha(4, "Aluno não encontrado."),
            new EnturmacaoLoteFalha(5, "Aluno já possui enturmação ativa."),
        ], result.Falhas);
        VerifyNothingPersisted(alunoTurmaRepository);
    }

    [Fact]
    public async Task EnturmarEmLoteAsync_QuandoSelecionadosExcedemVagas_ReturnsConflict()
    {
        var (service, alunoRepository, alunoTurmaRepository) = BuildService();
        alunoRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync([BuildAluno(1), BuildAluno(2), BuildAluno(3)]);
        alunoTurmaRepository.Setup(x => x.GetOcupacaoMaximaAsync(1, Hoje)).ReturnsAsync(28);

        var result = await service.EnturmarEmLoteAsync(new EnturmacaoLoteRequest { TurmaId = 1, DataInicio = Hoje, AlunoIds = [1, 2, 3] });

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Conflict, result.Error);
        Assert.Equal("A turma possui 2 vagas disponíveis, mas 3 alunos foram selecionados.", result.Message);
        VerifyNothingPersisted(alunoTurmaRepository);
    }

    [Fact]
    public async Task EnturmarEmLoteAsync_QuandoValido_EnturmaTodosDeUmaVez()
    {
        var (service, alunoRepository, alunoTurmaRepository) = BuildService();
        alunoRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync([BuildAluno(1), BuildAluno(2)]);
        alunoTurmaRepository
            .Setup(x => x.EnturmarAsync(It.IsAny<IReadOnlyCollection<int>>(), 1, Hoje))
            .ReturnsAsync(true);

        var result = await service.EnturmarEmLoteAsync(new EnturmacaoLoteRequest { TurmaId = 1, DataInicio = Hoje, AlunoIds = [1, 2, 2] });

        Assert.True(result.Success);
        Assert.Equal("2 alunos enturmados com sucesso na turma Turma 1!", result.Message);
        alunoTurmaRepository.Verify(x => x.EnturmarAsync(
            It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 1, 2 })), 1, Hoje), Times.Once);
    }

    [Fact]
    public async Task EnturmarEmLoteAsync_QuandoVagasAcabamDuranteAGravacao_ReturnsConflict()
    {
        var (service, alunoRepository, alunoTurmaRepository) = BuildService();
        alunoRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync([BuildAluno(1)]);
        alunoTurmaRepository
            .Setup(x => x.EnturmarAsync(It.IsAny<IReadOnlyCollection<int>>(), 1, Hoje))
            .ReturnsAsync(false);

        var result = await service.EnturmarEmLoteAsync(new EnturmacaoLoteRequest { TurmaId = 1, DataInicio = Hoje, AlunoIds = [1] });

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Conflict, result.Error);
    }

    private static void VerifyNothingPersisted(Mock<IAlunoTurmaRepository> alunoTurmaRepository)
        => alunoTurmaRepository.Verify(x => x.EnturmarAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<int>(), It.IsAny<DateOnly>()), Times.Never);

    private static (RemanejamentoAlunoService Service, Mock<IAlunoRepository> AlunoRepository, Mock<IAlunoTurmaRepository> AlunoTurmaRepository) BuildService()
    {
        var alunoRepository = new Mock<IAlunoRepository>();
        var alunoTurmaRepository = new Mock<IAlunoTurmaRepository>();
        var turmaRepository = new Mock<ITurmaRepository>();
        turmaRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(BuildTurma());
        alunoTurmaRepository.Setup(x => x.GetAlunoIdsComEnturmacaoAtivaAsync(It.IsAny<IReadOnlyCollection<int>>())).ReturnsAsync([]);
        return (new RemanejamentoAlunoService(alunoRepository.Object, alunoTurmaRepository.Object, turmaRepository.Object), alunoRepository, alunoTurmaRepository);
    }

    private static Aluno BuildAluno(int id, int escolaId = 1, string status = Aluno.StatusAtivoAguardandoEnturmacao)
        => new() { Id = id, EscolaId = escolaId, Status = status };

    private static Turma BuildTurma() => new()
    {
        Id = 1,
        EscolaId = 1,
        AnoLetivoId = 1,
        NomeCompleto = "Turma 1",
        Status = Turma.StatusAtivo,
        VagasOfertadas = 30,
        AnoLetivo = new AnoLetivo
        {
            Id = 1,
            AnoReferencia = DateTime.Today.Year,
            DataInicio = new DateOnly(DateTime.Today.Year, 1, 1),
            DataTermino = new DateOnly(DateTime.Today.Year, 12, 31)
        }
    };
}
