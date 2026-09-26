using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiarioX.Server.Infrastructure.Data.Configurations;

// Faturamento da plataforma: entidades globais (sem tenant_id filtrado), acessadas pelo Administrador global.

public class PlanoAssinaturaConfiguration : IEntityTypeConfiguration<PlanoAssinatura>
{
    public void Configure(EntityTypeBuilder<PlanoAssinatura> builder)
    {
        builder.ToTable("planos_assinatura", t =>
            t.HasCheckConstraint("CK_planos_assinatura_valores",
                "valor_fixo >= 0 AND valor_por_aluno >= 0 AND valor_minimo >= 0"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(500);
        builder.Property(x => x.ValorFixo).HasColumnName("valor_fixo").HasPrecision(12, 2);
        builder.Property(x => x.ValorPorAluno).HasColumnName("valor_por_aluno").HasPrecision(12, 2);
        builder.Property(x => x.ValorMinimo).HasColumnName("valor_minimo").HasPrecision(12, 2);
        builder.Property(x => x.Ativo).HasColumnName("ativo").HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => x.Nome).HasDatabaseName("IX_planos_assinatura_nome").IsUnique();
    }
}

public class AssinaturaConfiguration : IEntityTypeConfiguration<Assinatura>
{
    public void Configure(EntityTypeBuilder<Assinatura> builder)
    {
        builder.ToTable("assinaturas", t =>
        {
            t.HasCheckConstraint("CK_assinaturas_situacao", "situacao IN ('TESTE', 'ATIVA', 'CANCELADA')");
            t.HasCheckConstraint("CK_assinaturas_dia_vencimento",
                $"dia_vencimento BETWEEN 1 AND {Assinatura.DiaVencimentoMaximo}");
            t.HasCheckConstraint("CK_assinaturas_desconto", "desconto_percentual BETWEEN 0 AND 100");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.PlanoId).HasColumnName("plano_id").IsRequired();
        builder.Property(x => x.Situacao).HasColumnName("situacao").HasMaxLength(20).IsRequired();
        builder.Property(x => x.DataInicio).HasColumnName("data_inicio").HasColumnType("date").IsRequired();
        builder.Property(x => x.TesteAte).HasColumnName("teste_ate").HasColumnType("date");
        builder.Property(x => x.DiaVencimento).HasColumnName("dia_vencimento").IsRequired();
        builder.Property(x => x.DescontoPercentual).HasColumnName("desconto_percentual").HasPrecision(5, 2);
        builder.Property(x => x.RazaoSocial).HasColumnName("razao_social").HasMaxLength(255).IsRequired();
        builder.Property(x => x.CpfCnpj).HasColumnName("cpf_cnpj").HasMaxLength(14).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(20);
        builder.Property(x => x.Cep).HasColumnName("cep").HasMaxLength(8);
        builder.Property(x => x.Endereco).HasColumnName("endereco").HasMaxLength(255);
        builder.Property(x => x.Numero).HasColumnName("numero").HasMaxLength(20);
        builder.Property(x => x.Complemento).HasColumnName("complemento").HasMaxLength(100);
        builder.Property(x => x.Bairro).HasColumnName("bairro").HasMaxLength(100);
        builder.Property(x => x.AsaasClienteId).HasColumnName("asaas_cliente_id").HasMaxLength(50);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Plano)
            .WithMany()
            .HasForeignKey(x => x.PlanoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("IX_assinaturas_tenant").IsUnique();
    }
}

public class FaturaAssinaturaConfiguration : IEntityTypeConfiguration<FaturaAssinatura>
{
    public void Configure(EntityTypeBuilder<FaturaAssinatura> builder)
    {
        builder.ToTable("faturas_assinatura", t =>
            t.HasCheckConstraint("CK_faturas_assinatura_situacao",
                "situacao IN ('PENDENTE', 'PAGA', 'VENCIDA', 'CANCELADA', 'ESTORNADA')"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(x => x.AssinaturaId).HasColumnName("assinatura_id").IsRequired();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.Competencia).HasColumnName("competencia").HasColumnType("date").IsRequired();
        builder.Property(x => x.Vencimento).HasColumnName("vencimento").HasColumnType("date").IsRequired();
        builder.Property(x => x.AlunosAtivos).HasColumnName("alunos_ativos");
        builder.Property(x => x.ValorFixo).HasColumnName("valor_fixo").HasPrecision(12, 2);
        builder.Property(x => x.ValorPorAluno).HasColumnName("valor_por_aluno").HasPrecision(12, 2);
        builder.Property(x => x.ValorMinimo).HasColumnName("valor_minimo").HasPrecision(12, 2);
        builder.Property(x => x.ValorCalculado).HasColumnName("valor_calculado").HasPrecision(12, 2);
        builder.Property(x => x.DescontoPercentual).HasColumnName("desconto_percentual").HasPrecision(5, 2);
        builder.Property(x => x.Valor).HasColumnName("valor").HasPrecision(12, 2);
        builder.Property(x => x.Situacao).HasColumnName("situacao").HasMaxLength(20).IsRequired();
        builder.Property(x => x.AsaasCobrancaId).HasColumnName("asaas_cobranca_id").HasMaxLength(50);
        builder.Property(x => x.LinkPagamento).HasColumnName("link_pagamento").HasMaxLength(500);
        builder.Property(x => x.PagaEm).HasColumnName("paga_em").HasColumnType("date");
        builder.Property(x => x.ValorPago).HasColumnName("valor_pago").HasPrecision(12, 2);
        builder.Property(x => x.FormaPagamento).HasColumnName("forma_pagamento").HasMaxLength(30);
        builder.Property(x => x.NotaFiscalId).HasColumnName("nota_fiscal_id").HasMaxLength(50);
        builder.Property(x => x.NotaFiscalSituacao).HasColumnName("nota_fiscal_situacao").HasMaxLength(20);
        builder.Property(x => x.NotaFiscalNumero).HasColumnName("nota_fiscal_numero").HasMaxLength(50);
        builder.Property(x => x.NotaFiscalPdfUrl).HasColumnName("nota_fiscal_pdf_url").HasMaxLength(500);
        builder.Property(x => x.NotaFiscalErro).HasColumnName("nota_fiscal_erro").HasMaxLength(500);
        builder.Property(x => x.Observacao).HasColumnName("observacao").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Assinatura)
            .WithMany()
            .HasForeignKey(x => x.AssinaturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Uma fatura válida por mês de referência; uma cancelada pode ser substituída por outra.
        builder.HasIndex(x => new { x.AssinaturaId, x.Competencia })
            .HasDatabaseName("IX_faturas_assinatura_competencia")
            .IsUnique()
            .HasFilter("situacao <> 'CANCELADA'");

        builder.HasIndex(x => x.AsaasCobrancaId).HasDatabaseName("IX_faturas_assinatura_asaas_cobranca");
        builder.HasIndex(x => x.NotaFiscalId).HasDatabaseName("IX_faturas_assinatura_nota_fiscal");
    }
}
