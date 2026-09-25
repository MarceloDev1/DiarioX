using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Tests.Infrastructure.Data;

/// <summary>
/// Exercita os filtros globais e as regras de gravação por instituição do AppDbContext.
/// </summary>
public class TenantIsolationTests
{
    private const int TenantA = 1;
    private const int TenantB = 2;

    [Fact]
    public void Query_ReturnsOnlyRowsOfCurrentTenant()
    {
        var database = SeedTwoTenants();

        using var contextA = CreateContext(database, TenantA);
        using var contextB = CreateContext(database, TenantB);

        Assert.Equal(["Modalidade A"], contextA.ModalidadesEnsino.Select(m => m.Nome).ToList());
        Assert.Equal(["Modalidade B"], contextB.ModalidadesEnsino.Select(m => m.Nome).ToList());
    }

    [Fact]
    public void Query_WithoutTenant_ReturnsNothingForTenantEntities()
    {
        var database = SeedTwoTenants();

        using var context = CreateContext(database, tenantId: null);

        Assert.Empty(context.ModalidadesEnsino.ToList());
    }

    [Fact]
    public async Task FindAsync_DoesNotReturnOtherTenantsRow()
    {
        var database = SeedTwoTenants();
        int idDoTenantB;
        using (var contextB = CreateContext(database, TenantB))
            idDoTenantB = contextB.ModalidadesEnsino.Single().Id;

        using var contextA = CreateContext(database, TenantA);

        Assert.Null(await contextA.ModalidadesEnsino.FindAsync(idDoTenantB));
    }

    [Fact]
    public void SaveChanges_StampsCurrentTenantOnInsert()
    {
        var database = SeedTwoTenants();

        using (var contextB = CreateContext(database, TenantB))
        {
            contextB.ModalidadesEnsino.Add(new ModalidadeEnsino { Nome = "Nova", Sigla = "NV" });
            contextB.SaveChanges();
        }

        using var system = CreateContext(database, tenantId: null);
        var nova = system.ModalidadesEnsino.IgnoreQueryFilters().Single(m => m.Nome == "Nova");
        Assert.Equal(TenantB, nova.TenantId);
    }

    [Fact]
    public void SaveChanges_WithoutTenant_RejectsInsertOfTenantEntity()
    {
        var database = SeedTwoTenants();

        using var context = CreateContext(database, tenantId: null);
        context.ModalidadesEnsino.Add(new ModalidadeEnsino { Nome = "Sem dono", Sigla = "SD" });

        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
    }

    [Fact]
    public void SaveChanges_RejectsUpdateOfOtherTenantsRow()
    {
        var database = SeedTwoTenants();
        ModalidadeEnsino doTenantA;
        using (var contextA = CreateContext(database, TenantA))
            doTenantA = contextA.ModalidadesEnsino.AsNoTracking().Single();

        using var contextB = CreateContext(database, TenantB);
        doTenantA.Nome = "Alterada por B";
        contextB.ModalidadesEnsino.Update(doTenantA);

        Assert.Throws<InvalidOperationException>(() => contextB.SaveChanges());
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsDeleteOfOtherTenantsRow()
    {
        var database = SeedTwoTenants();
        ModalidadeEnsino doTenantA;
        using (var contextA = CreateContext(database, TenantA))
            doTenantA = contextA.ModalidadesEnsino.AsNoTracking().Single();

        using var contextB = CreateContext(database, TenantB);
        contextB.ModalidadesEnsino.Remove(doTenantA);

        await Assert.ThrowsAsync<InvalidOperationException>(() => contextB.SaveChangesAsync());
    }

    [Fact]
    public void Users_GlobalAdminsAreVisibleOnlyWithoutTenant()
    {
        var database = SeedTwoTenants();

        using var global = CreateContext(database, tenantId: null);
        using var contextA = CreateContext(database, TenantA);

        Assert.Equal(["admin@diariox.local"], global.Users.Select(u => u.Email).ToList());
        Assert.Equal(["usuario@a.com"], contextA.Users.Select(u => u.Email).ToList());
    }

    [Fact]
    public void Users_CreatedInsideTenantBelongToIt_AndOutsideAreGlobal()
    {
        var database = SeedTwoTenants();

        using (var contextB = CreateContext(database, TenantB))
        {
            contextB.Users.Add(new User { Email = "novo@b.com", Cpf = "11144477735" });
            contextB.SaveChanges();
        }

        using (var global = CreateContext(database, tenantId: null))
        {
            global.Users.Add(new User { Email = "outro-admin@diariox.local", Cpf = "52998224725" });
            global.SaveChanges();
        }

        using var system = CreateContext(database, tenantId: null);
        var users = system.Users.IgnoreQueryFilters().ToDictionary(u => u.Email, u => u.TenantId);
        Assert.Equal(TenantB, users["novo@b.com"]);
        Assert.Null(users["outro-admin@diariox.local"]);
    }

    [Fact]
    public void UsuariosPerfis_FollowTheTenantOfTheUser()
    {
        var database = SeedTwoTenants();

        using var contextA = CreateContext(database, TenantA);
        using var contextB = CreateContext(database, TenantB);

        Assert.Single(contextA.UsuariosPerfis.ToList());
        Assert.Empty(contextB.UsuariosPerfis.ToList());
    }

    private static string SeedTwoTenants()
    {
        var database = Guid.NewGuid().ToString();

        using var system = CreateContext(database, tenantId: null);

        system.Tenants.AddRange(
            new Tenant { Id = TenantA, Nome = "Instituição A", Slug = "a" },
            new Tenant { Id = TenantB, Nome = "Instituição B", Slug = "b" });

        // Fora de uma instituição, entidades de tenant precisam informar o TenantId explicitamente.
        system.ModalidadesEnsino.AddRange(
            new ModalidadeEnsino { TenantId = TenantA, Nome = "Modalidade A", Sigla = "MA" },
            new ModalidadeEnsino { TenantId = TenantB, Nome = "Modalidade B", Sigla = "MB" });

        var perfil = new Perfil { Nome = Perfil.Professor, Descricao = "Professor" };
        var usuarioA = new User { TenantId = TenantA, Email = "usuario@a.com", Cpf = "11144477735" };
        system.Perfis.Add(perfil);
        system.Users.AddRange(
            new User { Email = "admin@diariox.local", Cpf = "00000000000" },
            usuarioA);
        system.UsuariosPerfis.Add(new UsuarioPerfil { Usuario = usuarioA, Perfil = perfil });

        system.SaveChanges();
        return database;
    }

    private static AppDbContext CreateContext(string database, int? tenantId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(database)
            .Options;

        return new AppDbContext(options, new FixedTenantContext(tenantId));
    }

    private sealed class FixedTenantContext(int? tenantId) : ITenantContext
    {
        public int? TenantId { get; } = tenantId;
        public string? TenantSlug => TenantId?.ToString();
    }
}
