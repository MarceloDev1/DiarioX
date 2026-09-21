using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

public class AlunoConfiguration : IEntityTypeConfiguration<Aluno>
{
    public void Configure(EntityTypeBuilder<Aluno> builder)
    {
        builder.ToTable("alunos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        // Foreign Keys
        builder.HasOne(x => x.Escola)
            .WithMany()
            .HasForeignKey(x => x.EscolaId)
            .HasConstraintName("FK_alunos_escolas")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(x => x.Matricula)
            .HasDatabaseName("IX_alunos_matricula")
            .IsUnique();

        // RN01: CPF único quando informado.
        builder.HasIndex(x => x.CpfAluno)
            .HasDatabaseName("IX_alunos_cpf_aluno")
            .IsUnique()
            .HasFilter("cpf_aluno IS NOT NULL");

        // RN01: quando o aluno não possui CPF, a chave de unicidade é Nome + Data de Nascimento + Responsável 1.
        builder.HasIndex(x => new { x.Nome, x.DataNascimento, x.ResponsavelNome1 })
            .HasDatabaseName("IX_alunos_nome_nascimento_responsavel")
            .IsUnique()
            .HasFilter("cpf_aluno IS NULL");

        builder.HasIndex(x => x.EscolaId)
            .HasDatabaseName("IX_alunos_escola_id");

        // Propriedades
        builder.Property(x => x.Matricula)
            .HasColumnName("matricula")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.DataNascimento)
            .HasColumnName("data_nascimento")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Sexo)
            .HasColumnName("sexo")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CorRaca)
            .HasColumnName("cor_raca")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.NecessidadeEspecial)
            .HasColumnName("necessidade_especial")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.ResponsavelNome1)
            .HasColumnName("responsavel_nome1")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ResponsavelCpf1)
            .HasColumnName("responsavel_cpf1")
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(x => x.ResponsavelTelefone1)
            .HasColumnName("responsavel_telefone1")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ResponsavelNome2)
            .HasColumnName("responsavel_nome2")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.CpfAluno)
            .HasColumnName("cpf_aluno")
            .HasMaxLength(11)
            .IsRequired(false);

        builder.Property(x => x.CertidaoNascimento)
            .HasColumnName("certidao_nascimento")
            .HasMaxLength(60)
            .IsRequired(false);

        builder.Property(x => x.Cep)
            .HasColumnName("cep")
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(x => x.EnderecoCompleto)
            .HasColumnName("endereco_completo")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Numero)
            .HasColumnName("numero")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Bairro)
            .HasColumnName("bairro")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(40)
            .HasDefaultValue(Aluno.StatusAtivoAguardandoEnturmacao)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        // Check Constraints
        builder.ToTable(t =>
            t.HasCheckConstraint("CK_alunos_sexo", "sexo IN ('MASCULINO', 'FEMININO')"));
    }
}
