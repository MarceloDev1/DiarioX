using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DiarioX.Server.Tests.API;

/// <summary>
/// Sobe a API real (Program.cs: middleware de tenant, políticas, controllers) com banco InMemory
/// e duas instituições ativas e uma inativa.
/// </summary>
public class TenancyApiFactory : WebApplicationFactory<Program>
{
    public const string TokenWebhookAsaas = "token-webhook-teste";

    private readonly string _databaseName = $"diariox-api-tests-{Guid.NewGuid()}";
    private readonly object _seedLock = new();
    private bool _seeded;

    public Tenant TenantA { get; } = new() { Nome = "Escola A", Slug = "escola-a" };
    public Tenant TenantB { get; } = new() { Nome = "Escola B", Slug = "escola-b" };
    public Tenant TenantInativo { get; } = new() { Nome = "Escola Inativa", Slug = "escola-inativa", Status = Tenant.StatusInativo };

    public int ModalidadeAId { get; private set; }
    public int ModalidadeBId { get; private set; }
    public int GlobalAdminId { get; private set; }
    public int UsuarioBId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development traz as configurações de Jwt e Tenancy:BaseDomains = dev.localhost.
        builder.UseEnvironment("Development");

        // Sem rotina de faturamento em segundo plano nos testes; token fixo para o webhook do Asaas.
        builder.UseSetting("Faturamento:RotinaHabilitada", "false");
        builder.UseSetting("Asaas:WebhookToken", TokenWebhookAsaas);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    public void EnsureSeeded()
    {
        lock (_seedLock)
        {
            if (_seeded)
                return;

            // Escopo sem requisição HTTP: área global, como o seed do Program.cs.
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Tenants.AddRange(TenantA, TenantB, TenantInativo);
            db.SaveChanges();

            var modalidadeA = new ModalidadeEnsino { TenantId = TenantA.Id, Nome = "Fundamental A", Sigla = "FA" };
            var modalidadeB = new ModalidadeEnsino { TenantId = TenantB.Id, Nome = "Fundamental B", Sigla = "FB" };
            var usuarioB = new User { TenantId = TenantB.Id, Email = "secretaria@escola-b.com", Cpf = "11144477735" };
            db.ModalidadesEnsino.AddRange(modalidadeA, modalidadeB);
            db.Users.Add(usuarioB);
            db.SaveChanges();

            ModalidadeAId = modalidadeA.Id;
            ModalidadeBId = modalidadeB.Id;
            UsuarioBId = usuarioB.Id;
            GlobalAdminId = db.Users.Single(u => u.Email == "admin@diariox.local").Id;
            _seeded = true;
        }
    }
}
