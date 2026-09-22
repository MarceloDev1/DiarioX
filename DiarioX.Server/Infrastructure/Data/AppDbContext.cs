using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Escola> Escolas => Set<Escola>();
    public DbSet<ModalidadeEnsino> ModalidadesEnsino => Set<ModalidadeEnsino>();
    public DbSet<EtapaEnsino> EtapasEnsino => Set<EtapaEnsino>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<UsuarioPerfil> UsuariosPerfis => Set<UsuarioPerfil>();
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
    public DbSet<ProfessorAlocacao> ProfessorAlocacoes => Set<ProfessorAlocacao>();
    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<AlunoTurma> AlunosTurmas => Set<AlunoTurma>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new EscolaConfiguration());
        modelBuilder.ApplyConfiguration(new ModalidadeEnsinoConfiguration());
        modelBuilder.ApplyConfiguration(new EtapaEnsinoConfiguration());
        modelBuilder.ApplyConfiguration(new PerfilConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioPerfilConfiguration());
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
        modelBuilder.ApplyConfiguration(new ProfessorAlocacaoConfiguration());
        modelBuilder.ApplyConfiguration(new AlunoConfiguration());
        modelBuilder.ApplyConfiguration(new AlunoTurmaConfiguration());
    }
}
