using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class RemanejamentoAlunoServiceTests
{
    [Fact]
    public async Task RemanejarAsync_WhenDestinationIsCurrentClass_ReturnsValidation()
    {
        var (service, _, turmaRepository) = BuildService();
        var origem = BuildVinculo();
        turmaRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(origem.Turma);

        var result = await service.RemanejarAsync(1, new RemanejamentoAlunoRequest
        {
            TurmaDestinoId = 1,
            DataMovimentacao = DateOnly.FromDateTime(DateTime.Today)
        });

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("A turma de destino deve ser diferente da turma atual do aluno.", result.Message);
    }

    [Fact]
    public async Task RemanejarAsync_WhenDateIsBeforeSchoolYear_ReturnsValidation()
    {
        var (service, _, turmaRepository) = BuildService();
        turmaRepository.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(BuildTurma(2));

        var result = await service.RemanejarAsync(1, new RemanejamentoAlunoRequest
        {
            TurmaDestinoId = 2,
            DataMovimentacao = new DateOnly(DateTime.Today.Year, 1, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("A data da movimentação deve estar entre o início do ano letivo e a data atual.", result.Message);
    }

    [Fact]
    public async Task RemanejarAsync_WhenDateMatchesEnrollmentStart_ReturnsValidation()
    {
        var (service, _, turmaRepository) = BuildService();
        turmaRepository.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(BuildTurma(2));

        var result = await service.RemanejarAsync(1, new RemanejamentoAlunoRequest
        {
            TurmaDestinoId = 2,
            DataMovimentacao = new DateOnly(DateTime.Today.Year, 2, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("A data da movimentação deve ser posterior ao início da enturmação atual.", result.Message);
    }

    [Fact]
    public async Task RemanejarAsync_WhenDestinationHasNoVacancy_ReturnsConflict()
    {
        var (service, alunoTurmaRepository, turmaRepository) = BuildService();
        turmaRepository.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(BuildTurma(2));
        alunoTurmaRepository.Setup(x => x.HasVacancyAsync(2, It.IsAny<DateOnly>())).ReturnsAsync(false);

        var result = await service.RemanejarAsync(1, new RemanejamentoAlunoRequest
        {
            TurmaDestinoId = 2,
            DataMovimentacao = DateOnly.FromDateTime(DateTime.Today)
        });

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Conflict, result.Error);
        Assert.Equal("A turma de destino não possui vagas disponíveis para remanejamento.", result.Message);
        alunoTurmaRepository.Verify(x => x.RemanejarAsync(It.IsAny<AlunoTurma>(), It.IsAny<int>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task RemanejarAsync_WhenValid_ClosesOriginAndCreatesDestinationLink()
    {
        var (service, alunoTurmaRepository, turmaRepository) = BuildService();
        var destino = BuildTurma(2);
        var data = DateOnly.FromDateTime(DateTime.Today);
        turmaRepository.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(destino);
        alunoTurmaRepository.Setup(x => x.HasVacancyAsync(2, data)).ReturnsAsync(true);

        var result = await service.RemanejarAsync(1, new RemanejamentoAlunoRequest
        {
            TurmaDestinoId = 2,
            DataMovimentacao = data
        });

        Assert.True(result.Success);
        Assert.Equal($"Aluno remanejado com sucesso para a turma {destino.NomeCompleto}!", result.Message);
        alunoTurmaRepository.Verify(x => x.RemanejarAsync(It.IsAny<AlunoTurma>(), 2, data), Times.Once);
    }

    [Fact]
    public async Task RemanejarAsync_WhenAlunoInativo_ReturnsValidation()
    {
        var (service, alunoTurmaRepository, _) = BuildService();
        var vinculo = BuildVinculo();
        vinculo.Aluno.Status = Aluno.StatusInativo;
        alunoTurmaRepository.Setup(x => x.GetAtivaByAlunoIdAsync(1)).ReturnsAsync(vinculo);

        var result = await service.RemanejarAsync(1, new RemanejamentoAlunoRequest
        {
            TurmaDestinoId = 2,
            DataMovimentacao = DateOnly.FromDateTime(DateTime.Today)
        });

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("Não é possível remanejar um aluno inativo.", result.Message);
    }

    private static (RemanejamentoAlunoService Service, Mock<IAlunoTurmaRepository> AlunoTurmaRepository, Mock<ITurmaRepository> TurmaRepository) BuildService()
    {
        var alunoRepository = new Mock<IAlunoRepository>();
        var alunoTurmaRepository = new Mock<IAlunoTurmaRepository>();
        var turmaRepository = new Mock<ITurmaRepository>();
        alunoTurmaRepository.Setup(x => x.GetAtivaByAlunoIdAsync(1)).ReturnsAsync(BuildVinculo());
        return (new RemanejamentoAlunoService(alunoRepository.Object, alunoTurmaRepository.Object, turmaRepository.Object), alunoTurmaRepository, turmaRepository);
    }

    private static AlunoTurma BuildVinculo() => new()
    {
        Id = 10,
        AlunoId = 1,
        DataInicio = new DateOnly(DateTime.Today.Year, 2, 1),
        Aluno = new Aluno { Id = 1, EscolaId = 1, Escola = new Escola { Id = 1, Nome = "Escola Modelo" } },
        TurmaId = 1,
        Turma = BuildTurma(1)
    };

    private static Turma BuildTurma(int id) => new()
    {
        Id = id,
        EscolaId = 1,
        AnoLetivoId = 1,
        NomeCompleto = $"Turma {id}",
        Status = Turma.StatusAtivo,
        VagasOfertadas = 30,
        AnoLetivo = new AnoLetivo
        {
            Id = 1,
            AnoReferencia = DateTime.Today.Year,
            DataInicio = new DateOnly(DateTime.Today.Year, 2, 1),
            DataTermino = new DateOnly(DateTime.Today.Year, 12, 20)
        }
    };
}