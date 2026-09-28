using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class RegraAvaliacaoConfiguration : IEntityTypeConfiguration<RegraAvaliacao>
{
    public void Configure(EntityTypeBuilder<RegraAvaliacao> builder)
    {
        builder.ToTable("regras_avaliacao", table =>
        {
            table.HasCheckConstraint("CK_regras_avaliacao_calculo", "calculo_nota_periodo IN ('MEDIA_PONDERADA', 'SOMA')");
            table.HasCheckConstraint("CK_regras_avaliacao_media", "media_aprovacao > 0 AND media_aprovacao <= nota_maxima");
            table.HasCheckConstraint("CK_regras_avaliacao_casas", $"casas_decimais BETWEEN 0 AND {RegraAvaliacao.CasasDecimaisMaximo}");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
        builder.Property(x => x.NotaMaxima).HasColumnName("nota_maxima").HasPrecision(7, 2).IsRequired();
        builder.Property(x => x.MediaAprovacao).HasColumnName("media_aprovacao").HasPrecision(7, 2).IsRequired();
        builder.Property(x => x.CasasDecimais).HasColumnName("casas_decimais").IsRequired();
        builder.Property(x => x.CalculoNotaPeriodo).HasColumnName("calculo_nota_periodo").HasMaxLength(20).IsRequired();
        builder.Property(x => x.PermiteRecuperacao).HasColumnName("permite_recuperacao").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        // Excluir a regra devolve as etapas à regra padrão do sistema.
        builder.HasMany(x => x.Etapas)
            .WithOne(e => e.RegraAvaliacao)
            .HasForeignKey(e => e.RegraAvaliacaoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.Nome })
            .HasDatabaseName("IX_regras_avaliacao_nome")
            .IsUnique();
    }
}
