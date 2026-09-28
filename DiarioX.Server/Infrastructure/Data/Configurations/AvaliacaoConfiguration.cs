using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class AvaliacaoConfiguration : IEntityTypeConfiguration<Avaliacao>
{
    public void Configure(EntityTypeBuilder<Avaliacao> builder)
    {
        builder.ToTable("avaliacoes", table =>
        {
            table.HasCheckConstraint("CK_avaliacoes_tipo",
                "tipo IN ('PROVA', 'TRABALHO', 'ATIVIDADE', 'PARTICIPACAO', 'OUTRO', 'RECUPERACAO')");
            table.HasCheckConstraint("CK_avaliacoes_peso", "peso > 0");
            table.HasCheckConstraint("CK_avaliacoes_valor_maximo", "valor_maximo IS NULL OR valor_maximo > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.TurmaId).HasColumnName("turma_id").IsRequired();
        builder.Property(x => x.DisciplinaId).HasColumnName("disciplina_id").IsRequired();
        builder.Property(x => x.PeriodoAvaliativoId).HasColumnName("periodo_avaliativo_id").IsRequired();
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(Avaliacao.MaxNome).IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Data).HasColumnName("data").HasColumnType("date");
        builder.Property(x => x.Peso).HasColumnName("peso").HasPrecision(6, 2).HasDefaultValue(1m).IsRequired();
        builder.Property(x => x.ValorMaximo).HasColumnName("valor_maximo").HasPrecision(7, 2);
        builder.Property(x => x.RegistradoPorUsuarioId).HasColumnName("registrado_por_usuario_id").IsRequired();
        builder.Property(x => x.AtualizadoPorUsuarioId).HasColumnName("atualizado_por_usuario_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(x => x.EhRecuperacao);

        builder.HasOne(x => x.Turma)
            .WithMany()
            .HasForeignKey(x => x.TurmaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Disciplina)
            .WithMany()
            .HasForeignKey(x => x.DisciplinaId)
            .OnDelete(DeleteBehavior.Restrict);

        // O período não pode ser removido do ano letivo enquanto tiver avaliações.
        builder.HasOne(x => x.PeriodoAvaliativo)
            .WithMany()
            .HasForeignKey(x => x.PeriodoAvaliativoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RegistradoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AtualizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.TurmaId, x.DisciplinaId, x.PeriodoAvaliativoId })
            .HasDatabaseName("IX_avaliacoes_turma_disciplina_periodo");

        // Uma recuperação por turma, disciplina e período.
        builder.HasIndex(x => new { x.TurmaId, x.DisciplinaId, x.PeriodoAvaliativoId })
            .HasDatabaseName("IX_avaliacoes_recuperacao_unica")
            .IsUnique()
            .HasFilter("tipo = 'RECUPERACAO'");
    }
}

public class NotaAvaliacaoConfiguration : IEntityTypeConfiguration<NotaAvaliacao>
{
    public void Configure(EntityTypeBuilder<NotaAvaliacao> builder)
    {
        builder.ToTable("notas_avaliacoes", table =>
            table.HasCheckConstraint("CK_notas_avaliacoes_valor", "valor >= 0"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.AvaliacaoId).HasColumnName("avaliacao_id").IsRequired();
        builder.Property(x => x.AlunoId).HasColumnName("aluno_id").IsRequired();
        builder.Property(x => x.Valor).HasColumnName("valor").HasPrecision(7, 2).IsRequired();
        builder.Property(x => x.LancadaPorUsuarioId).HasColumnName("lancada_por_usuario_id").IsRequired();
        builder.Property(x => x.LancadaEm).HasColumnName("lancada_em").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Avaliacao)
            .WithMany(a => a.Notas)
            .HasForeignKey(x => x.AvaliacaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Aluno)
            .WithMany()
            .HasForeignKey(x => x.AlunoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.LancadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AvaliacaoId, x.AlunoId })
            .HasDatabaseName("IX_notas_avaliacoes_avaliacao_aluno")
            .IsUnique();

        builder.HasIndex(x => x.AlunoId).HasDatabaseName("IX_notas_avaliacoes_aluno");
    }
}
