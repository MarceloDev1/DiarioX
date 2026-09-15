using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class DisciplinaEtapaEnsinoConfiguration : IEntityTypeConfiguration<DisciplinaEtapaEnsino>
{
    public void Configure(EntityTypeBuilder<DisciplinaEtapaEnsino> builder)
    {
        builder.ToTable("disciplinas_etapas_ensino");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.DisciplinaId)
            .HasColumnName("disciplina_id")
            .IsRequired();

        builder.Property(x => x.EtapaEnsinoId)
            .HasColumnName("etapa_ensino_id")
            .IsRequired();

        builder.HasOne(x => x.Disciplina)
            .WithMany(d => d.EtapasEnsino)
            .HasForeignKey(x => x.DisciplinaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EtapaEnsino)
            .WithMany()
            .HasForeignKey(x => x.EtapaEnsinoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DisciplinaId, x.EtapaEnsinoId })
            .HasDatabaseName("IX_disciplinas_etapas_ensino_unicidade")
            .IsUnique();
    }
}
