using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("email_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.TemplateId)
            .HasColumnName("template_id");

        builder.Property(x => x.DestinatarioEmail)
            .HasColumnName("destinatario_email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.DestinatarioNome)
            .HasColumnName("destinatario_nome")
            .HasMaxLength(255);

        builder.Property(x => x.Assunto)
            .HasColumnName("assunto")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.CorpoHtml)
            .HasColumnName("corpo_html")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(EmailLog.StatusPendente)
            .IsRequired();

        builder.Property(x => x.Tentativas)
            .HasColumnName("tentativas")
            .HasDefaultValue(0);

        builder.Property(x => x.ErroMensagem)
            .HasColumnName("erro_mensagem")
            .HasColumnType("text");

        builder.Property(x => x.EnviadoEm)
            .HasColumnName("enviado_em");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Template)
            .WithMany(t => t.EmailLogs)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_email_logs_status");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_email_logs_created_at");

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_email_logs_status", "status IN ('PENDENTE', 'ENVIADO', 'FALHOU')"));
    }
}
