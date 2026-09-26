using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(255)
            .IsRequired();

        // Um rótulo DNS tem no máximo 63 caracteres.
        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(63)
            .IsRequired();

        builder.HasIndex(x => x.Slug)
            .HasDatabaseName("IX_tenants_slug")
            .IsUnique();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(Tenant.StatusAtivo)
            .IsRequired();

        builder.Property(x => x.SituacaoFinanceira)
            .HasColumnName("situacao_financeira")
            .HasMaxLength(20)
            .HasDefaultValue(Tenant.SituacaoFinanceiraRegular)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_tenants_status", "status IN ('ATIVO', 'INATIVO')");
            t.HasCheckConstraint("CK_tenants_situacao_financeira",
                "situacao_financeira IN ('REGULAR', 'EM_ATRASO', 'SOMENTE_LEITURA')");
        });
    }
}
