using DiarioX.Server.Application.DTOs.Tenants;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class TenantServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenValid_NormalizesAndPersists()
    {
        var fixture = BuildService();
        fixture.TenantRepository
            .Setup(r => r.AddAsync(It.IsAny<Tenant>()))
            .ReturnsAsync((Tenant t) => { t.Id = 10; return t; });

        var result = await fixture.Service.CreateAsync(new TenantRequest { Nome = "  Colégio X ", Slug = " Colegio-X ", Status = "ativo" });

        Assert.True(result.Success);
        Assert.Equal(10, result.Tenant!.Id);
        Assert.Equal("Colégio X", result.Tenant.Nome);
        Assert.Equal("colegio-x", result.Tenant.Slug);
        Assert.Equal(Tenant.StatusAtivo, result.Tenant.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("colegio x")]
    [InlineData("-colegio")]
    [InlineData("colegio-")]
    [InlineData("colégio")]
    [InlineData("rede.sul")]
    public async Task CreateAsync_WhenSlugInvalid_ReturnsValidationError(string slug)
    {
        var fixture = BuildService();

        var result = await fixture.Service.CreateAsync(new TenantRequest { Nome = "Colégio X", Slug = slug });

        Assert.False(result.Success);
        Assert.Equal(TenantResultError.Validation, result.Error);
        fixture.TenantRepository.Verify(r => r.AddAsync(It.IsAny<Tenant>()), Times.Never);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("www")]
    public async Task CreateAsync_WhenSlugReserved_ReturnsValidationError(string slug)
    {
        var fixture = BuildService();

        var result = await fixture.Service.CreateAsync(new TenantRequest { Nome = "Colégio X", Slug = slug });

        Assert.False(result.Success);
        Assert.Equal(TenantResultError.Validation, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WhenSlugExists_ReturnsConflict()
    {
        var fixture = BuildService();
        fixture.TenantRepository.Setup(r => r.ExistsBySlugAsync("colegio-x", null)).ReturnsAsync(true);

        var result = await fixture.Service.CreateAsync(new TenantRequest { Nome = "Colégio X", Slug = "colegio-x" });

        Assert.False(result.Success);
        Assert.Equal(TenantResultError.Conflict, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenTenantMissing_ReturnsNotFound()
    {
        var fixture = BuildService();
        fixture.TenantRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Tenant?)null);

        var result = await fixture.Service.UpdateAsync(99, new TenantRequest { Nome = "Colégio X", Slug = "colegio-x" });

        Assert.False(result.Success);
        Assert.Equal(TenantResultError.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenSlugUsedByAnotherTenant_ReturnsConflict()
    {
        var fixture = BuildService();
        fixture.TenantRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tenant { Id = 1, Nome = "A", Slug = "a" });
        fixture.TenantRepository.Setup(r => r.ExistsBySlugAsync("b", 1)).ReturnsAsync(true);

        var result = await fixture.Service.UpdateAsync(1, new TenantRequest { Nome = "A", Slug = "b" });

        Assert.False(result.Success);
        Assert.Equal(TenantResultError.Conflict, result.Error);
        fixture.TenantRepository.Verify(r => r.UpdateAsync(It.IsAny<Tenant>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenNoTenantInContext_ReturnsNull()
    {
        var fixture = BuildService(currentTenantId: null);

        var result = await fixture.Service.GetCurrentAsync();

        Assert.Null(result);
        fixture.TenantRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenTenantInContext_ReturnsPublicData()
    {
        var fixture = BuildService(currentTenantId: 4);
        fixture.TenantRepository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(new Tenant { Id = 4, Nome = "Rede Y", Slug = "rede-y" });

        var result = await fixture.Service.GetCurrentAsync();

        Assert.NotNull(result);
        Assert.Equal("Rede Y", result!.Nome);
        Assert.Equal("rede-y", result.Slug);
    }

    private static (TenantService Service, Mock<ITenantRepository> TenantRepository) BuildService(int? currentTenantId = null)
    {
        var tenantRepository = new Mock<ITenantRepository>();
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(c => c.TenantId).Returns(currentTenantId);

        return (new TenantService(tenantRepository.Object, tenantContext.Object, new Mock<IPermissaoService>().Object), tenantRepository);
    }
}
