using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class ProfessorDisciplinaConfiguration : IEntityTypeConfiguration<ProfessorDisciplina>
{
    public void Configure(EntityTypeBuilder<ProfessorDisciplina> builder)
    {
        builder.ToTable("professor_disciplinas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        // Foreign Keys
        builder.HasOne(x => x.Professor)
            .WithMany(x => x.ProfessorDisciplinas)
            .HasForeignKey(x => x.ProfessorId)
            .HasConstraintName("FK_professor_disciplinas_professores")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Disciplina)
            .WithMany()
            .HasForeignKey(x => x.DisciplinaId)
            .HasConstraintName("FK_professor_disciplinas_disciplinas")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(x => x.ProfessorId)
            .HasDatabaseName("IX_professor_disciplinas_professor_id");

        builder.HasIndex(x => x.DisciplinaId)
            .HasDatabaseName("IX_professor_disciplinas_disciplina_id");

        builder.HasIndex(x => new { x.ProfessorId, x.DisciplinaId })
            .HasDatabaseName("IX_professor_disciplinas_unique")
            .IsUnique();

        // Propriedades
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");
    }
}
