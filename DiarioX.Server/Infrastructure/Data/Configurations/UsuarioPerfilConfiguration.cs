using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class UsuarioPerfilConfiguration : IEntityTypeConfiguration<UsuarioPerfil>
{
    public void Configure(EntityTypeBuilder<UsuarioPerfil> builder)
    {
        builder.ToTable("usuarios_perfis");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(x => x.PerfilId)
            .HasColumnName("perfil_id")
            .IsRequired();

        builder.Property(x => x.EscolaId)
            .HasColumnName("escola_id");

        builder.HasOne(x => x.Usuario)
            .WithMany(u => u.UsuariosPerfis)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Perfil)
            .WithMany()
            .HasForeignKey(x => x.PerfilId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Escola)
            .WithMany()
            .HasForeignKey(x => x.EscolaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Perfil global: usuario+perfil únicos quando escola_id é null
        builder.HasIndex(x => new { x.UsuarioId, x.PerfilId })
            .HasDatabaseName("IX_usuarios_perfis_global")
            .IsUnique()
            .HasFilter("escola_id IS NULL");

        // Perfil por escola: usuario+perfil+escola únicos
        builder.HasIndex(x => new { x.UsuarioId, x.PerfilId, x.EscolaId })
            .HasDatabaseName("IX_usuarios_perfis_escola")
            .IsUnique()
            .HasFilter("escola_id IS NOT NULL");
    }
}
