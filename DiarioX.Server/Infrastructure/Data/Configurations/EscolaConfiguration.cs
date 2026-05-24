using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class EscolaConfiguration : IEntityTypeConfiguration<Escola>
{
    public void Configure(EntityTypeBuilder<Escola> builder)
    {
        builder.ToTable("escolas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.HasIndex(x => x.CodigoInep)
            .HasDatabaseName("IX_escolas_codigo_inep")
            .IsUnique();

        builder.Property(x => x.CodigoInep)
            .HasColumnName("codigo_inep")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Cnpj)
            .HasColumnName("cnpj")
            .HasMaxLength(14)
            .IsRequired();

        builder.Property(x => x.Telefone)
            .HasColumnName("telefone")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.EmailInstitucional)
            .HasColumnName("email_institucional")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Municipio)
            .HasColumnName("municipio")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EnderecoCompleto)
            .HasColumnName("endereco_completo")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.DiretorId)
            .HasColumnName("diretor_id")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue(Escola.StatusAtivo)
            .IsRequired();

        builder.HasOne(x => x.Diretor)
            .WithMany()
            .HasForeignKey(x => x.DiretorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_escolas_status", "status IN ('ATIVO', 'INATIVO')"));
    }
}