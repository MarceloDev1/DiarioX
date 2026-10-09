using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class HorarioAulaConfiguration : IEntityTypeConfiguration<HorarioAula>
{
    public void Configure(EntityTypeBuilder<HorarioAula> builder)
    {
        builder.ToTable("horarios_aula");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.TurmaId).HasColumnName("turma_id").IsRequired();
        builder.Property(x => x.DiaSemana).HasColumnName("dia_semana").IsRequired();
        builder.Property(x => x.Ordem).HasColumnName("ordem").IsRequired();
        builder.Property(x => x.DisciplinaId).HasColumnName("disciplina_id").IsRequired();

        // A grade pertence à turma: sai junto com ela.
        builder.HasOne(x => x.Turma)
            .WithMany()
            .HasForeignKey(x => x.TurmaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Disciplina)
            .WithMany()
            .HasForeignKey(x => x.DisciplinaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Um tempo de aula ocupa uma única disciplina.
        builder.HasIndex(x => new { x.TenantId, x.TurmaId, x.DiaSemana, x.Ordem })
            .HasDatabaseName("IX_horarios_aula_turma_dia_ordem")
            .IsUnique();
    }
}
