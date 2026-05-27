using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("email_templates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.Nome)
            .HasDatabaseName("IX_email_templates_nome")
            .IsUnique();

        builder.Property(x => x.Assunto)
            .HasColumnName("assunto")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.CorpoHtml)
            .HasColumnName("corpo_html")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Variaveis)
            .HasColumnName("variaveis")
            .HasColumnType("text[]")
            .HasDefaultValueSql("'{}'::text[]");

        builder.Property(x => x.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");
    }
}
