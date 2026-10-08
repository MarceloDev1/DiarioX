using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class ConteudoMinistradoConfiguration : IEntityTypeConfiguration<ConteudoMinistrado>
{
    public void Configure(EntityTypeBuilder<ConteudoMinistrado> builder)
    {
        builder.ToTable("conteudos_ministrados");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.TurmaId).HasColumnName("turma_id").IsRequired();
        builder.Property(x => x.DisciplinaId).HasColumnName("disciplina_id").IsRequired();
        builder.Property(x => x.Data).HasColumnName("data").HasColumnType("date").IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(ConteudoMinistrado.MaxDescricao).IsRequired();
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

        // Um registro de conteúdo por turma, disciplina e dia (como a chamada).
        builder.HasIndex(x => new { x.TenantId, x.TurmaId, x.DisciplinaId, x.Data })
            .HasDatabaseName("IX_conteudos_ministrados_turma_disciplina_data")
            .IsUnique();
    }
}

public class ConteudoMinistradoHabilidadeConfiguration : IEntityTypeConfiguration<ConteudoMinistradoHabilidade>
{
    public void Configure(EntityTypeBuilder<ConteudoMinistradoHabilidade> builder)
    {
        builder.ToTable("conteudos_ministrados_habilidades");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.ConteudoMinistradoId).HasColumnName("conteudo_ministrado_id").IsRequired();
        builder.Property(x => x.HabilidadeBnccId).HasColumnName("habilidade_bncc_id").IsRequired();

        builder.HasOne(x => x.ConteudoMinistrado)
            .WithMany(c => c.Habilidades)
            .HasForeignKey(x => x.ConteudoMinistradoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.HabilidadeBncc)
            .WithMany()
            .HasForeignKey(x => x.HabilidadeBnccId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ConteudoMinistradoId, x.HabilidadeBnccId })
            .HasDatabaseName("IX_conteudos_ministrados_habilidades_unicidade")
            .IsUnique();

        builder.HasIndex(x => x.HabilidadeBnccId).HasDatabaseName("IX_conteudos_ministrados_habilidades_habilidade");
    }
}
