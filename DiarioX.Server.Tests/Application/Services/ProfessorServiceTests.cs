using Moq;
using Xunit;
using DiarioX.Server.Application.DTOs.Professores;
using DiarioX.Server.Application.DTOs.Users;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DiarioX.Server.Tests.Application.Services;

public class ProfessorServiceTests
{
    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsProfessor()
    {
        // Arrange
        var profRepository = new Mock<IProfessorRepository>();
        var disciplinaRepository = new Mock<IDisciplinaRepository>();
        var escolaRepository = new Mock<IEscolaRepository>();
        var userService = new Mock<IUserService>();
        var logger = new Mock<ILogger<ProfessorService>>();

        var professor = new Professor
        {
            Id = 1,
            Nome = "João Silva",
            Cpf = "11144477735",
            Email = "joao@escola.com",
            Telefone = "11987654321",
            Matricula = "MAT-001",
            DataNascimento = new DateTime(1985, 5, 15),
            DataAdmissao = new DateTime(2020, 1, 15),
            Situacao = "ATIVO",
            EscolaId = 1,
            CreatedAt = DateTime.UtcNow,
            Escola = new Escola { Id = 1, Nome = "Escola A", Cnpj = "123", Status = "ATIVO" },
            ProfessorDisciplinas = new List<ProfessorDisciplina>(),
            Usuario = null
        };

        profRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(professor);

        var service = new ProfessorService(profRepository.Object, disciplinaRepository.Object,
            escolaRepository.Object, userService.Object, logger.Object);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Professor);
        Assert.Equal("João Silva", result.Professor.Nome);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesProfessor()
    {
        // Arrange
        var profRepository = new Mock<IProfessorRepository>();
        var disciplinaRepository = new Mock<IDisciplinaRepository>();
        var escolaRepository = new Mock<IEscolaRepository>();
        var userService = new Mock<IUserService>();
        var logger = new Mock<ILogger<ProfessorService>>();

        var professor = new Professor
        {
            Id = 1,
            Nome = "João Silva"
        };

        profRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(professor);

        profRepository.Setup(r => r.DeleteAsync(It.IsAny<Professor>()))
            .Returns(Task.CompletedTask);

        profRepository.Setup(r => r.RemoveDisciplinasAsync(1))
            .Returns(Task.CompletedTask);

        var service = new ProfessorService(profRepository.Object, disciplinaRepository.Object,
            escolaRepository.Object, userService.Object, logger.Object);

        // Act
        var result = await service.DeleteAsync(1);

        // Assert
        Assert.True(result.Success);
        profRepository.Verify(r => r.DeleteAsync(It.IsAny<Professor>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var profRepository = new Mock<IProfessorRepository>();
        var disciplinaRepository = new Mock<IDisciplinaRepository>();
        var escolaRepository = new Mock<IEscolaRepository>();
        var userService = new Mock<IUserService>();
        var logger = new Mock<ILogger<ProfessorService>>();

        profRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Professor)null);

        var service = new ProfessorService(profRepository.Object, disciplinaRepository.Object,
            escolaRepository.Object, userService.Object, logger.Object);

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ProfessorResultError.NotFound, result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var profRepository = new Mock<IProfessorRepository>();
        var disciplinaRepository = new Mock<IDisciplinaRepository>();
        var escolaRepository = new Mock<IEscolaRepository>();
        var userService = new Mock<IUserService>();
        var logger = new Mock<ILogger<ProfessorService>>();

        profRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Professor)null);

        var service = new ProfessorService(profRepository.Object, disciplinaRepository.Object,
            escolaRepository.Object, userService.Object, logger.Object);

        // Act
        var result = await service.DeleteAsync(999);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ProfessorResultError.NotFound, result.Error);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProfessors()
    {
        // Arrange
        var profRepository = new Mock<IProfessorRepository>();
        var disciplinaRepository = new Mock<IDisciplinaRepository>();
        var escolaRepository = new Mock<IEscolaRepository>();
        var userService = new Mock<IUserService>();
        var logger = new Mock<ILogger<ProfessorService>>();

        var professors = new List<Professor>
        {
            new Professor
            {
                Id = 1,
                Nome = "João Silva",
                Cpf = "11144477735",
                Email = "joao@escola.com",
                Telefone = "11987654321",
                Matricula = "MAT-001",
                DataNascimento = new DateTime(1985, 5, 15),
                DataAdmissao = new DateTime(2020, 1, 15),
                Situacao = "ATIVO",
                EscolaId = 1,
                CreatedAt = DateTime.UtcNow,
                Escola = new Escola { Id = 1, Nome = "Escola A", Cnpj = "123", Status = "ATIVO" },
                ProfessorDisciplinas = new List<ProfessorDisciplina>(),
                Usuario = null
            },
            new Professor
            {
                Id = 2,
                Nome = "Maria Santos",
                Cpf = "22255588899",
                Email = "maria@escola.com",
                Telefone = "11987654322",
                Matricula = "MAT-002",
                DataNascimento = new DateTime(1990, 3, 20),
                DataAdmissao = new DateTime(2021, 2, 10),
                Situacao = "ATIVO",
                EscolaId = 1,
                CreatedAt = DateTime.UtcNow,
                Escola = new Escola { Id = 1, Nome = "Escola A", Cnpj = "123", Status = "ATIVO" },
                ProfessorDisciplinas = new List<ProfessorDisciplina>(),
                Usuario = null
            }
        };

        profRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(professors);

        var service = new ProfessorService(profRepository.Object, disciplinaRepository.Object,
            escolaRepository.Object, userService.Object, logger.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByEscolaIdAsync_ReturnsProfessorsForEscola()
    {
        // Arrange
        var profRepository = new Mock<IProfessorRepository>();
        var disciplinaRepository = new Mock<IDisciplinaRepository>();
        var escolaRepository = new Mock<IEscolaRepository>();
        var userService = new Mock<IUserService>();
        var logger = new Mock<ILogger<ProfessorService>>();

        var professors = new List<Professor>
        {
            new Professor
            {
                Id = 1,
                Nome = "João Silva",
                Cpf = "11144477735",
                Email = "joao@escola.com",
                Telefone = "11987654321",
                Matricula = "MAT-001",
                DataNascimento = new DateTime(1985, 5, 15),
                DataAdmissao = new DateTime(2020, 1, 15),
                Situacao = "ATIVO",
                EscolaId = 1,
                CreatedAt = DateTime.UtcNow,
                Escola = new Escola { Id = 1, Nome = "Escola A", Cnpj = "123", Status = "ATIVO" },
                ProfessorDisciplinas = new List<ProfessorDisciplina>(),
                Usuario = null
            }
        };

        profRepository.Setup(r => r.GetByEscolaIdAsync(1))
            .ReturnsAsync(professors);

        var service = new ProfessorService(profRepository.Object, disciplinaRepository.Object,
            escolaRepository.Object, userService.Object, logger.Object);

        // Act
        var result = await service.GetByEscolaIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("João Silva", result.First().Nome);
    }
}

