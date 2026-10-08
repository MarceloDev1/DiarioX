using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class HabilidadeBnccConfiguration : IEntityTypeConfiguration<HabilidadeBncc>
{
    public void Configure(EntityTypeBuilder<HabilidadeBncc> builder)
    {
        builder.ToTable("habilidades_bncc");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.DisciplinaId).HasColumnName("disciplina_id").IsRequired();
        builder.Property(x => x.Ativa).HasColumnName("ativa").HasDefaultValue(true).IsRequired();

        builder.HasOne(x => x.Disciplina)
            .WithMany()
            .HasForeignKey(x => x.DisciplinaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.EtapasEnsino)
            .WithOne(e => e.HabilidadeBncc)
            .HasForeignKey(e => e.HabilidadeBnccId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.Codigo })
            .HasDatabaseName("IX_habilidades_bncc_codigo")
            .IsUnique();

        builder.HasIndex(x => x.DisciplinaId).HasDatabaseName("IX_habilidades_bncc_disciplina");
    }
}

public class HabilidadeBnccEtapaEnsinoConfiguration : IEntityTypeConfiguration<HabilidadeBnccEtapaEnsino>
{
    public void Configure(EntityTypeBuilder<HabilidadeBnccEtapaEnsino> builder)
    {
        builder.ToTable("habilidades_bncc_etapas_ensino");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.HabilidadeBnccId).HasColumnName("habilidade_bncc_id").IsRequired();
        builder.Property(x => x.EtapaEnsinoId).HasColumnName("etapa_ensino_id").IsRequired();

        builder.HasOne(x => x.EtapaEnsino)
            .WithMany()
            .HasForeignKey(x => x.EtapaEnsinoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.HabilidadeBnccId, x.EtapaEnsinoId })
            .HasDatabaseName("IX_habilidades_bncc_etapas_ensino_unicidade")
            .IsUnique();

        builder.HasIndex(x => x.EtapaEnsinoId).HasDatabaseName("IX_habilidades_bncc_etapas_ensino_etapa");
    }
}
