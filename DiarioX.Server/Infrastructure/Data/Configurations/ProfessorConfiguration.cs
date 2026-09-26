using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class ProfessorConfiguration : IEntityTypeConfiguration<Professor>
{
    public void Configure(EntityTypeBuilder<Professor> builder)
    {
        builder.ToTable("professores");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        // Foreign Keys
        builder.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .HasConstraintName("FK_professores_usuarios")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Índices (unicidade dentro da instituição: o mesmo professor pode atuar em outra rede)
        builder.HasIndex(x => new { x.TenantId, x.Cpf })
            .HasDatabaseName("IX_professores_cpf")
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Matricula })
            .HasDatabaseName("IX_professores_matricula")
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Email })
            .HasDatabaseName("IX_professores_email")
            .IsUnique();

        // Propriedades
        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Cpf)
            .HasColumnName("cpf")
            .HasMaxLength(14)
            .IsRequired();

        builder.Property(x => x.DataNascimento)
            .HasColumnName("data_nascimento")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Telefone)
            .HasColumnName("telefone")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Matricula)
            .HasColumnName("matricula")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.DataAdmissao)
            .HasColumnName("data_admissao")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(x => x.Situacao)
            .HasColumnName("situacao")
            .HasMaxLength(20)
            .HasDefaultValue(Professor.StatusAtivo)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        // Check Constraints
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_professores_situacao",
                "situacao IN ('ATIVO', 'INATIVO', 'AFASTADO', 'LICENCIADO')"));
    }
}
