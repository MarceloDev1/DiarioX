using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class ModalidadeEnsinoConfiguration : IEntityTypeConfiguration<ModalidadeEnsino>
{
    public void Configure(EntityTypeBuilder<ModalidadeEnsino> builder)
    {
        builder.ToTable("modalidades_ensino");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.HasIndex(x => x.Nome)
            .HasDatabaseName("IX_modalidades_ensino_nome")
            .IsUnique();

        builder.HasIndex(x => x.Sigla)
            .HasDatabaseName("IX_modalidades_ensino_sigla")
            .IsUnique();

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Sigla)
            .HasColumnName("sigla")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CodigoMec)
            .HasColumnName("codigo_mec")
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(ModalidadeEnsino.StatusAtivo)
            .IsRequired();

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_modalidades_ensino_status", "status IN ('ATIVO', 'INATIVO')"));
    }
}
