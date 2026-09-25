using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class ProfessorEscolaConfiguration : IEntityTypeConfiguration<ProfessorEscola>
{
    public void Configure(EntityTypeBuilder<ProfessorEscola> builder)
    {
        builder.ToTable("professor_escolas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        // Foreign Keys
        builder.HasOne(x => x.Professor)
            .WithMany(x => x.ProfessorEscolas)
            .HasForeignKey(x => x.ProfessorId)
            .HasConstraintName("FK_professor_escolas_professores")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Escola)
            .WithMany()
            .HasForeignKey(x => x.EscolaId)
            .HasConstraintName("FK_professor_escolas_escolas")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(x => x.ProfessorId)
            .HasDatabaseName("IX_professor_escolas_professor_id");

        builder.HasIndex(x => x.EscolaId)
            .HasDatabaseName("IX_professor_escolas_escola_id");

        builder.HasIndex(x => new { x.ProfessorId, x.EscolaId })
            .HasDatabaseName("IX_professor_escolas_unique")
            .IsUnique();

        // Propriedades
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");
    }
}
