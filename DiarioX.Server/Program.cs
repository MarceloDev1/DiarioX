using System.Text;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using DiarioX.Server.Infrastructure.Repositories;
using DiarioX.Server.Infrastructure.Services;
using DiarioX.Server.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Permite informar no Swagger o token de /api/auth/login (ou /api/auth/select-tenant).
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// Multitenancy: a instituição da requisição é resolvida pelo TenantResolutionMiddleware
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddSingleton<TenantHostResolver>();
builder.Services.AddScoped<IAppUrlProvider, AppUrlProvider>();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection - Repositories
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEscolaRepository, EscolaRepository>();
builder.Services.AddScoped<IModalidadeEnsinoRepository, ModalidadeEnsinoRepository>();
builder.Services.AddScoped<IEtapaEnsinoRepository, EtapaEnsinoRepository>();
builder.Services.AddScoped<IPerfilRepository, PerfilRepository>();
builder.Services.AddScoped<IUsuarioPerfilRepository, UsuarioPerfilRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IAnoLetivoRepository, AnoLetivoRepository>();
builder.Services.AddScoped<ITurmaRepository, TurmaRepository>();
builder.Services.AddScoped<IDisciplinaRepository, DisciplinaRepository>();
builder.Services.AddScoped<IProfessorRepository, ProfessorRepository>();
builder.Services.AddScoped<IProfessorAlocacaoRepository, ProfessorAlocacaoRepository>();
builder.Services.AddScoped<IAlunoRepository, AlunoRepository>();
builder.Services.AddScoped<IAlunoTurmaRepository, AlunoTurmaRepository>();

// Dependency Injection - Services
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEscolaService, EscolaService>();
builder.Services.AddScoped<IModalidadeEnsinoService, ModalidadeEnsinoService>();
builder.Services.AddScoped<IEtapaEnsinoService, EtapaEnsinoService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpClient<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<IAnoLetivoService, AnoLetivoService>();
builder.Services.AddScoped<ITurmaService, TurmaService>();
builder.Services.AddScoped<IDisciplinaService, DisciplinaService>();
builder.Services.AddScoped<IProfessorService, ProfessorService>();
builder.Services.AddScoped<IProfessorAlocacaoService, ProfessorAlocacaoService>();
builder.Services.AddScoped<IAlunoService, AlunoService>();
builder.Services.AddScoped<IRemanejamentoAlunoService, RemanejamentoAlunoService>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Mantém os nomes originais das claims (sub, tenant_id...) em vez de mapear para ClaimTypes.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Toda rota exige usuário autenticado atuando em uma instituição, salvo [AllowAnonymous]
    // ou política explícita. Um Administrador global sem instituição selecionada só acessa
    // as rotas da política GlobalAdmin (seleção e gestão de instituições).
    var tenantPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim(AppClaimTypes.TenantId)
        .Build();

    options.DefaultPolicy = tenantPolicy;
    options.FallbackPolicy = tenantPolicy;

    options.AddPolicy(AppPolicies.GlobalAdmin, policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim(AppClaimTypes.GlobalAdmin, "true"));
});

var app = builder.Build();

// Database initialization (roda sem instituição: os filtros enxergam apenas usuários globais)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Nos testes de integração o banco é InMemory, que não tem migrations.
    if (db.Database.IsRelational())
        db.Database.Migrate();

    // Seed admin user
    if (!db.Users.Any(u => u.Email == "admin@diariox.local"))
    {
        var adminUser = new User
        {
            Email = "admin@diariox.local",
            Cpf = "00000000000",
            Status = User.StatusAtivo,
        };
        adminUser.SetPassword("admin123");
        db.Users.Add(adminUser);
        db.SaveChanges();
    }

    // Garante que o admin padrão tenha o perfil global de Administrador
    var admin = db.Users.First(u => u.Email == "admin@diariox.local");
    var perfilAdministrador = db.Perfis.FirstOrDefault(p => p.Nome.ToLower() == Perfil.Administrador.ToLower());
    if (perfilAdministrador is not null &&
        !db.UsuariosPerfis.Any(up => up.UsuarioId == admin.Id && up.PerfilId == perfilAdministrador.Id && up.EscolaId == null))
    {
        db.UsuariosPerfis.Add(new UsuarioPerfil
        {
            UsuarioId = admin.Id,
            PerfilId = perfilAdministrador.Id,
            EscolaId = null,
        });
        db.SaveChanges();
    }
}

app.UseDefaultFiles();
app.MapStaticAssets().AllowAnonymous();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html").AllowAnonymous();

app.Run();
