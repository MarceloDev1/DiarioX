using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class PeriodoAvaliativoConfiguration : IEntityTypeConfiguration<PeriodoAvaliativo>
{
    public void Configure(EntityTypeBuilder<PeriodoAvaliativo> builder)
    {
        builder.ToTable("periodos_avaliativos");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AnoLetivoId).HasColumnName("ano_letivo_id").IsRequired();
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Numero).HasColumnName("numero").IsRequired();
        builder.Property(x => x.DataInicio).HasColumnName("data_inicio").HasColumnType("date").IsRequired();
        builder.Property(x => x.DataTermino).HasColumnName("data_termino").HasColumnType("date").IsRequired();

        builder.HasOne(x => x.AnoLetivo)
            .WithMany(a => a.Periodos)
            .HasForeignKey(x => x.AnoLetivoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.AnoLetivoId, x.Numero })
            .HasDatabaseName("IX_periodos_avaliativos_ano_numero")
            .IsUnique();
    }
}
