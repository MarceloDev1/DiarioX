using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class PerfilPermissaoConfiguration : IEntityTypeConfiguration<PerfilPermissao>
{
    public void Configure(EntityTypeBuilder<PerfilPermissao> builder)
    {
        builder.ToTable("perfis_permissoes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.PerfilId)
            .HasColumnName("perfil_id")
            .IsRequired();

        builder.Property(x => x.Permissao)
            .HasColumnName("permissao")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(x => x.Perfil)
            .WithMany()
            .HasForeignKey(x => x.PerfilId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.PerfilId, x.Permissao })
            .HasDatabaseName("IX_perfis_permissoes_tenant_perfil_permissao")
            .IsUnique();
    }
}
