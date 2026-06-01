using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class TurmaConfiguration : IEntityTypeConfiguration<Turma>
{
    public void Configure(EntityTypeBuilder<Turma> builder)
    {
        builder.ToTable("turmas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.AnoLetivoId)
            .HasColumnName("ano_letivo_id")
            .IsRequired();

        builder.Property(x => x.EscolaId)
            .HasColumnName("escola_id")
            .IsRequired();

        builder.Property(x => x.ModalidadeEnsinoId)
            .HasColumnName("modalidade_ensino_id")
            .IsRequired();

        builder.Property(x => x.EtapaEnsinoId)
            .HasColumnName("etapa_ensino_id")
            .IsRequired();

        builder.Property(x => x.NomeIdentificador)
            .HasColumnName("nome_identificador")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.NomeCompleto)
            .HasColumnName("nome_completo")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Turno)
            .HasColumnName("turno")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.VagasOfertadas)
            .HasColumnName("vagas_ofertadas")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(Turma.StatusAtivo)
            .IsRequired();

        builder.HasOne(x => x.AnoLetivo)
            .WithMany()
            .HasForeignKey(x => x.AnoLetivoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Escola)
            .WithMany()
            .HasForeignKey(x => x.EscolaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModalidadeEnsino)
            .WithMany()
            .HasForeignKey(x => x.ModalidadeEnsinoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EtapaEnsino)
            .WithMany()
            .HasForeignKey(x => x.EtapaEnsinoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AnoLetivoId, x.EscolaId, x.EtapaEnsinoId, x.NomeIdentificador, x.Turno })
            .HasDatabaseName("IX_turmas_unicidade")
            .IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_turmas_status", "status IN ('ATIVO', 'INATIVO')");
            t.HasCheckConstraint("CK_turmas_turno", "turno IN ('MANHA', 'TARDE', 'NOITE', 'INTEGRAL')");
            t.HasCheckConstraint("CK_turmas_vagas_ofertadas", "vagas_ofertadas > 0");
        });
    }
}