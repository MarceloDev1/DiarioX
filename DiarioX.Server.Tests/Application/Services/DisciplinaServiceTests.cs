using DiarioX.Server.Application.DTOs.Disciplinas;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class DisciplinaServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenEtapaNotVisible_ReturnsValidationError()
    {
        var fixture = BuildService();
        // Etapa de outra instituição: o repositório (filtrado por tenant) não a encontra.
        fixture.EtapaRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((EtapaEnsino?)null);

        var result = await fixture.Service.CreateAsync(BuildRequest(99));

        Assert.False(result.Success);
        Assert.Equal(DisciplinaResultError.Validation, result.Error);
        fixture.DisciplinaRepository.Verify(r => r.AddAsync(It.IsAny<Disciplina>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenEtapasExist_Persists()
    {
        var fixture = BuildService();
        fixture.EtapaRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new EtapaEnsino { Id = 1, Nome = "1º Ano" });
        fixture.DisciplinaRepository
            .Setup(r => r.AddAsync(It.IsAny<Disciplina>()))
            .ReturnsAsync((Disciplina d) => { d.Id = 3; return d; });

        var result = await fixture.Service.CreateAsync(BuildRequest(1));

        Assert.True(result.Success);
        fixture.DisciplinaRepository.Verify(r => r.AddAsync(It.Is<Disciplina>(d => d.EtapasEnsino.Single().EtapaEnsinoId == 1)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenEtapaNotVisible_ReturnsValidationError()
    {
        var fixture = BuildService();
        fixture.DisciplinaRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Disciplina { Id = 3, Nome = "Matemática", Codigo = "MAT" });
        fixture.EtapaRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((EtapaEnsino?)null);

        var result = await fixture.Service.UpdateAsync(3, BuildRequest(99));

        Assert.False(result.Success);
        Assert.Equal(DisciplinaResultError.Validation, result.Error);
        fixture.DisciplinaRepository.Verify(r => r.UpdateAsync(It.IsAny<Disciplina>()), Times.Never);
    }

    private static DisciplinaRequest BuildRequest(params int[] etapaIds) => new()
    {
        Nome = "Matemática",
        Codigo = "MAT",
        Descricao = "Disciplina de matemática",
        EtapasEnsinoIds = etapaIds.ToList(),
    };

    private static (DisciplinaService Service, Mock<IDisciplinaRepository> DisciplinaRepository, Mock<IEtapaEnsinoRepository> EtapaRepository) BuildService()
    {
        var disciplinaRepository = new Mock<IDisciplinaRepository>();
        var etapaRepository = new Mock<IEtapaEnsinoRepository>();
        return (new DisciplinaService(disciplinaRepository.Object, etapaRepository.Object), disciplinaRepository, etapaRepository);
    }
}
