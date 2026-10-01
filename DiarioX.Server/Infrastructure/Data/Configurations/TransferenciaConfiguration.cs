using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class TransferenciaConfiguration : IEntityTypeConfiguration<Transferencia>
{
    public void Configure(EntityTypeBuilder<Transferencia> builder)
    {
        builder.ToTable("transferencias", table =>
            table.HasCheckConstraint("CK_transferencias_tipo",
                "tipo IN ('OUTRA_REDE', 'ENTRE_ESCOLAS_DA_REDE', 'MUDANCA_MUNICIPIO_ESTADO')"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.AlunoId).HasColumnName("aluno_id").IsRequired();
        builder.Property(x => x.EscolaOrigemId).HasColumnName("escola_origem_id").IsRequired();
        builder.Property(x => x.TurmaId).HasColumnName("turma_id");
        builder.Property(x => x.AnoLetivoId).HasColumnName("ano_letivo_id").IsRequired();
        builder.Property(x => x.DataTransferencia).HasColumnName("data_transferencia").HasColumnType("date").IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(40).IsRequired();
        builder.Property(x => x.EscolaDestino).HasColumnName("escola_destino").HasMaxLength(Transferencia.MaxEscolaDestino).IsRequired();
        builder.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(Transferencia.MaxMotivo);
        builder.Property(x => x.RegistradoPorUsuarioId).HasColumnName("registrado_por_usuario_id").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Aluno).WithMany().HasForeignKey(x => x.AlunoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.EscolaOrigem).WithMany().HasForeignKey(x => x.EscolaOrigemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Turma).WithMany().HasForeignKey(x => x.TurmaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AnoLetivo).WithMany().HasForeignKey(x => x.AnoLetivoId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AlunoId, x.DataTransferencia }).HasDatabaseName("IX_transferencias_aluno_data");
    }
}
