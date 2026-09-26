using System.Reflection;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private static readonly MethodInfo ConfigureTenantEntityMethod = typeof(AppDbContext)
        .GetMethod(nameof(ConfigureTenantEntity), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Avaliado a cada consulta pelos filtros globais. Nulo = área global (sem instituição).
    public int? CurrentTenantId => _tenantContext.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Escola> Escolas => Set<Escola>();
    public DbSet<ModalidadeEnsino> ModalidadesEnsino => Set<ModalidadeEnsino>();
    public DbSet<EtapaEnsino> EtapasEnsino => Set<EtapaEnsino>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<UsuarioPerfil> UsuariosPerfis => Set<UsuarioPerfil>();
    public DbSet<PerfilPermissao> PerfisPermissoes => Set<PerfilPermissao>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AnoLetivo> AnosLetivos => Set<AnoLetivo>();
    public DbSet<PeriodoAvaliativo> PeriodosAvaliativos => Set<PeriodoAvaliativo>();
    public DbSet<Turma> Turmas => Set<Turma>();
    public DbSet<Disciplina> Disciplinas => Set<Disciplina>();
    public DbSet<DisciplinaEtapaEnsino> DisciplinasEtapasEnsino => Set<DisciplinaEtapaEnsino>();
    public DbSet<Professor> Professores => Set<Professor>();
    public DbSet<ProfessorDisciplina> ProfessorDisciplinas => Set<ProfessorDisciplina>();
    public DbSet<ProfessorEscola> ProfessorEscolas => Set<ProfessorEscola>();
    public DbSet<ProfessorAlocacao> ProfessorAlocacoes => Set<ProfessorAlocacao>();
    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<AlunoTurma> AlunosTurmas => Set<AlunoTurma>();
    public DbSet<Chamada> Chamadas => Set<Chamada>();
    public DbSet<ChamadaAluno> ChamadasAlunos => Set<ChamadaAluno>();
    public DbSet<PlanoAssinatura> PlanosAssinatura => Set<PlanoAssinatura>();
    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();
    public DbSet<FaturaAssinatura> FaturasAssinatura => Set<FaturaAssinatura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new EscolaConfiguration());
        modelBuilder.ApplyConfiguration(new ModalidadeEnsinoConfiguration());
        modelBuilder.ApplyConfiguration(new EtapaEnsinoConfiguration());
        modelBuilder.ApplyConfiguration(new PerfilConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioPerfilConfiguration());
        modelBuilder.ApplyConfiguration(new PerfilPermissaoConfiguration());
        modelBuilder.ApplyConfiguration(new EmailTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new EmailLogConfiguration());
        modelBuilder.ApplyConfiguration(new PasswordResetTokenConfiguration());
        modelBuilder.ApplyConfiguration(new AnoLetivoConfiguration());
        modelBuilder.ApplyConfiguration(new PeriodoAvaliativoConfiguration());
        modelBuilder.ApplyConfiguration(new TurmaConfiguration());
        modelBuilder.ApplyConfiguration(new DisciplinaConfiguration());
        modelBuilder.ApplyConfiguration(new DisciplinaEtapaEnsinoConfiguration());
        modelBuilder.ApplyConfiguration(new ProfessorConfiguration());
        modelBuilder.ApplyConfiguration(new ProfessorDisciplinaConfiguration());
        modelBuilder.ApplyConfiguration(new ProfessorEscolaConfiguration());
        modelBuilder.ApplyConfiguration(new ProfessorAlocacaoConfiguration());
        modelBuilder.ApplyConfiguration(new AlunoConfiguration());
        modelBuilder.ApplyConfiguration(new AlunoTurmaConfiguration());
        modelBuilder.ApplyConfiguration(new ChamadaConfiguration());
        modelBuilder.ApplyConfiguration(new ChamadaAlunoConfiguration());
        modelBuilder.ApplyConfiguration(new PlanoAssinaturaConfiguration());
        modelBuilder.ApplyConfiguration(new AssinaturaConfiguration());
        modelBuilder.ApplyConfiguration(new FaturaAssinaturaConfiguration());

        ConfigureTenantFilters(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ConfigureTenantFilters(ModelBuilder modelBuilder)
    {
        var tenantEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Select(t => t.ClrType)
            .Where(typeof(ITenantEntity).IsAssignableFrom)
            .ToList();

        foreach (var clrType in tenantEntityTypes)
            ConfigureTenantEntityMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);

        // Usuário com TenantId nulo é Administrador global: só aparece fora de uma instituição
        // (login no host de administração, seed). Os demais só aparecem na própria instituição.
        modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId == CurrentTenantId);

        // Perfis e tokens de senha seguem o tenant do usuário a que pertencem.
        modelBuilder.Entity<UsuarioPerfil>().HasQueryFilter(up => up.Usuario.TenantId == CurrentTenantId);
        modelBuilder.Entity<PasswordResetToken>().HasQueryFilter(t => t.User.TenantId == CurrentTenantId);
    }

    private void ConfigureTenantEntity<T>(ModelBuilder modelBuilder) where T : class, ITenantEntity
    {
        var builder = modelBuilder.Entity<T>();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sem instituição resolvida (CurrentTenantId nulo) a consulta não retorna nada.
        builder.HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }

    /// <summary>
    /// Preenche o TenantId das inclusões com a instituição da requisição e impede gravar
    /// dados de outra instituição. Sem instituição (área global/seed), inclusões de entidades
    /// de instituição precisam informar o TenantId explicitamente.
    /// </summary>
    private void ApplyTenantRules()
    {
        var tenantId = CurrentTenantId;

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == 0)
            {
                entry.Entity.TenantId = tenantId
                    ?? throw new InvalidOperationException(
                        $"Não é possível gravar {entry.Metadata.ClrType.Name} sem uma instituição selecionada.");
            }
            else if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                     tenantId is not null && entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    $"{entry.Metadata.ClrType.Name} pertence a outra instituição.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId is null)
            {
                // Usuário criado dentro de uma instituição pertence a ela; fora dela, é global.
                entry.Entity.TenantId = tenantId;
            }
            else if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                     tenantId is not null && entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException("Usuário pertence a outra instituição.");
            }
        }
    }
}
