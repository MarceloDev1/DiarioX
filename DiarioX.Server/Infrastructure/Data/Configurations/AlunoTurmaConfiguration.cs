using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class AlunoTurmaConfiguration : IEntityTypeConfiguration<AlunoTurma>
{
    public void Configure(EntityTypeBuilder<AlunoTurma> builder)
    {
        builder.ToTable("alunos_turmas", table =>
        {
            // data_fim = data_inicio - 1: vínculo desfeito no mesmo dia em que começaria (ver AlunoTurma.DataFim).
            table.HasCheckConstraint("CK_alunos_turmas_periodo", "data_fim IS NULL OR data_fim >= data_inicio - 1");
            table.HasCheckConstraint("CK_alunos_turmas_motivo_desenturmacao",
                "motivo_desenturmacao IS NULL OR motivo_desenturmacao IN " +
                "('REESTRUTURACAO_INTERNA', 'NAO_COMPARECEU', 'FALECIMENTO', 'ERRO_MATRICULA_ENTURMACAO', 'OUTROS', 'TRANSFERENCIA')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.AlunoId).HasColumnName("aluno_id").IsRequired();
        builder.Property(x => x.TurmaId).HasColumnName("turma_id").IsRequired();
        builder.Property(x => x.DataInicio).HasColumnName("data_inicio").HasColumnType("date").IsRequired();
        builder.Property(x => x.DataFim).HasColumnName("data_fim").HasColumnType("date");
        builder.Property(x => x.MotivoDesenturmacao).HasColumnName("motivo_desenturmacao").HasMaxLength(40);
        builder.Property(x => x.ObservacaoDesenturmacao).HasColumnName("observacao_desenturmacao")
            .HasMaxLength(AlunoTurma.MaxObservacaoDesenturmacao);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Aluno)
            .WithMany()
            .HasForeignKey(x => x.AlunoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Turma)
            .WithMany()
            .HasForeignKey(x => x.TurmaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AlunoId, x.DataInicio }).HasDatabaseName("IX_alunos_turmas_aluno_inicio");
        builder.HasIndex(x => new { x.TurmaId, x.DataInicio, x.DataFim }).HasDatabaseName("IX_alunos_turmas_turma_periodo");
        builder.HasIndex(x => x.AlunoId).HasDatabaseName("IX_alunos_turmas_aluno_ativo").IsUnique().HasFilter("data_fim IS NULL");
    }
}