using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class AnoLetivoConfiguration : IEntityTypeConfiguration<AnoLetivo>
{
    public void Configure(EntityTypeBuilder<AnoLetivo> builder)
    {
        builder.ToTable("anos_letivos", t => t.HasCheckConstraint("CK_anos_letivos_tipo_periodo",
            "tipo_periodo IN ('BIMESTRAL', 'TRIMESTRAL', 'SEMESTRAL')"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AnoReferencia).HasColumnName("ano_referencia").IsRequired();
        builder.Property(x => x.DataInicio).HasColumnName("data_inicio").HasColumnType("date").IsRequired();
        builder.Property(x => x.DataTermino).HasColumnName("data_termino").HasColumnType("date").IsRequired();
        builder.Property(x => x.TipoPeriodo).HasColumnName("tipo_periodo").HasMaxLength(20).IsRequired();

        builder.HasIndex(x => x.AnoReferencia)
            .HasDatabaseName("IX_anos_letivos_ano_referencia")
            .IsUnique();
    }
}
