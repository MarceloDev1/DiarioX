using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class DisciplinaConfiguration : IEntityTypeConfiguration<Disciplina>
{
    public void Configure(EntityTypeBuilder<Disciplina> builder)
    {
        builder.ToTable("disciplinas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(500);

        builder.Property(x => x.Ativa)
            .HasColumnName("ativa")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasMany(x => x.EtapasEnsino)
            .WithOne(de => de.Disciplina)
            .HasForeignKey(de => de.DisciplinaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.Codigo })
            .HasDatabaseName("IX_disciplinas_codigo")
            .IsUnique();

        builder.HasIndex(x => x.Nome)
            .HasDatabaseName("IX_disciplinas_nome");
    }
}
