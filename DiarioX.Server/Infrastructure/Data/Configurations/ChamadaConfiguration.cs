using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class ChamadaConfiguration : IEntityTypeConfiguration<Chamada>
{
    public void Configure(EntityTypeBuilder<Chamada> builder)
    {
        builder.ToTable("chamadas", table =>
            table.HasCheckConstraint("CK_chamadas_quantidade_aulas",
                $"quantidade_aulas BETWEEN 1 AND {Chamada.MaxQuantidadeAulas}"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.TurmaId).HasColumnName("turma_id").IsRequired();
        builder.Property(x => x.DisciplinaId).HasColumnName("disciplina_id").IsRequired();
        builder.Property(x => x.Data).HasColumnName("data").HasColumnType("date").IsRequired();
        builder.Property(x => x.QuantidadeAulas).HasColumnName("quantidade_aulas").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.Conteudo).HasColumnName("conteudo").HasMaxLength(2000);
        builder.Property(x => x.RegistradoPorUsuarioId).HasColumnName("registrado_por_usuario_id").IsRequired();
        builder.Property(x => x.AtualizadoPorUsuarioId).HasColumnName("atualizado_por_usuario_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Turma)
            .WithMany()
            .HasForeignKey(x => x.TurmaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Disciplina)
            .WithMany()
            .HasForeignKey(x => x.DisciplinaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RegistradoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AtualizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Uma chamada por turma, disciplina e dia (aulas seguidas entram em QuantidadeAulas).
        builder.HasIndex(x => new { x.TenantId, x.TurmaId, x.DisciplinaId, x.Data })
            .HasDatabaseName("IX_chamadas_turma_disciplina_data")
            .IsUnique();
    }
}

public class ChamadaAlunoConfiguration : IEntityTypeConfiguration<ChamadaAluno>
{
    public void Configure(EntityTypeBuilder<ChamadaAluno> builder)
    {
        builder.ToTable("chamadas_alunos", table =>
            table.HasCheckConstraint("CK_chamadas_alunos_situacao",
                "situacao IN ('PRESENTE', 'FALTA', 'FALTA_JUSTIFICADA')"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.ChamadaId).HasColumnName("chamada_id").IsRequired();
        builder.Property(x => x.AlunoId).HasColumnName("aluno_id").IsRequired();
        builder.Property(x => x.Situacao).HasColumnName("situacao").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Justificativa).HasColumnName("justificativa").HasMaxLength(255);

        builder.HasOne(x => x.Chamada)
            .WithMany(c => c.Registros)
            .HasForeignKey(x => x.ChamadaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Aluno)
            .WithMany()
            .HasForeignKey(x => x.AlunoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ChamadaId, x.AlunoId })
            .HasDatabaseName("IX_chamadas_alunos_chamada_aluno")
            .IsUnique();

        builder.HasIndex(x => x.AlunoId).HasDatabaseName("IX_chamadas_alunos_aluno");
    }
}
