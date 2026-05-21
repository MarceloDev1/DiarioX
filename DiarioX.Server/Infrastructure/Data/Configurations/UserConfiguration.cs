using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("IX_users_email")
            .IsUnique();

        builder.HasIndex(x => x.Cpf)
            .HasDatabaseName("IX_users_cpf")
            .IsUnique();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Cpf)
            .HasColumnName("cpf")
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(x => x.SenhaHash)
            .HasColumnName("senha_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(User.StatusAtivo)
            .IsRequired();

        builder.Property(x => x.UltimoAcesso)
            .HasColumnName("ultimo_acesso");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_users_status", "status IN ('ATIVO', 'INATIVO', 'BLOQUEADO')"));
    }
}
