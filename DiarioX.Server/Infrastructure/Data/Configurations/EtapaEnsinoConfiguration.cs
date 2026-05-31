using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class EtapaEnsinoConfiguration : IEntityTypeConfiguration<EtapaEnsino>
{
    public void Configure(EntityTypeBuilder<EtapaEnsino> builder)
    {
        builder.ToTable("etapas_ensino");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.ModalidadeEnsinoId)
            .HasColumnName("modalidade_ensino_id")
            .IsRequired();

        builder.HasOne(x => x.ModalidadeEnsino)
            .WithMany()
            .HasForeignKey(x => x.ModalidadeEnsinoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Sigla)
            .HasColumnName("sigla")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.OrdemCronologica)
            .HasColumnName("ordem_cronologica")
            .IsRequired();

        builder.Property(x => x.IdadeRecomendada)
            .HasColumnName("idade_recomendada")
            .IsRequired(false);

        builder.HasIndex(x => x.Sigla)
            .HasDatabaseName("IX_etapas_ensino_sigla")
            .IsUnique();

        builder.HasIndex(x => new { x.ModalidadeEnsinoId, x.Nome })
            .HasDatabaseName("IX_etapas_ensino_modalidade_nome")
            .IsUnique();

        builder.HasIndex(x => new { x.ModalidadeEnsinoId, x.OrdemCronologica })
            .HasDatabaseName("IX_etapas_ensino_modalidade_ordem")
            .IsUnique();
    }
}
