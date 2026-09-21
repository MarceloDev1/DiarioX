using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class ProfessorAlocacaoConfiguration : IEntityTypeConfiguration<ProfessorAlocacao>
{
    public void Configure(EntityTypeBuilder<ProfessorAlocacao> builder)
    {
        builder.ToTable("professor_alocacoes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.ProfessorId).HasColumnName("professor_id").IsRequired();
        builder.Property(x => x.TurmaId).HasColumnName("turma_id").IsRequired();
        builder.Property(x => x.DisciplinaId).HasColumnName("disciplina_id").IsRequired();
        builder.Property(x => x.Ativa).HasColumnName("ativa").HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Professor)
            .WithMany()
            .HasForeignKey(x => x.ProfessorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Turma)
            .WithMany()
            .HasForeignKey(x => x.TurmaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Disciplina)
            .WithMany()
            .HasForeignKey(x => x.DisciplinaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ProfessorId, x.TurmaId, x.DisciplinaId })
            .IsUnique()
            .HasDatabaseName("IX_professor_alocacoes_professor_turma_disciplina");

        builder.HasIndex(x => new { x.TurmaId, x.DisciplinaId })
            .IsUnique()
            .HasFilter("ativa = TRUE")
            .HasDatabaseName("IX_professor_alocacoes_turma_disciplina_ativa");
    }
}