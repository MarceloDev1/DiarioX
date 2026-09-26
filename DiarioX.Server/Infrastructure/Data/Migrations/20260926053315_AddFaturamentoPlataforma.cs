using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaturamentoPlataforma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "situacao_financeira",
                table: "tenants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "REGULAR");

            migrationBuilder.CreateTable(
                name: "planos_assinatura",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    valor_fixo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_por_aluno = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_minimo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planos_assinatura", x => x.id);
                    table.CheckConstraint("CK_planos_assinatura_valores", "valor_fixo >= 0 AND valor_por_aluno >= 0 AND valor_minimo >= 0");
                });

            migrationBuilder.CreateTable(
                name: "assinaturas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    plano_id = table.Column<int>(type: "integer", nullable: false),
                    situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    teste_ate = table.Column<DateOnly>(type: "date", nullable: true),
                    dia_vencimento = table.Column<int>(type: "integer", nullable: false),
                    desconto_percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    razao_social = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    cpf_cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    endereco = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    asaas_cliente_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assinaturas", x => x.id);
                    table.CheckConstraint("CK_assinaturas_desconto", "desconto_percentual BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_assinaturas_dia_vencimento", "dia_vencimento BETWEEN 1 AND 28");
                    table.CheckConstraint("CK_assinaturas_situacao", "situacao IN ('TESTE', 'ATIVA', 'CANCELADA')");
                    table.ForeignKey(
                        name: "FK_assinaturas_planos_assinatura_plano_id",
                        column: x => x.plano_id,
                        principalTable: "planos_assinatura",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assinaturas_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "faturas_assinatura",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    assinatura_id = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    alunos_ativos = table.Column<int>(type: "integer", nullable: false),
                    valor_fixo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_por_aluno = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_minimo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_calculado = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    desconto_percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    asaas_cobranca_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    link_pagamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    paga_em = table.Column<DateOnly>(type: "date", nullable: true),
                    valor_pago = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    forma_pagamento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    nota_fiscal_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    nota_fiscal_situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    nota_fiscal_numero = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    nota_fiscal_pdf_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    nota_fiscal_erro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faturas_assinatura", x => x.id);
                    table.CheckConstraint("CK_faturas_assinatura_situacao", "situacao IN ('PENDENTE', 'PAGA', 'VENCIDA', 'CANCELADA', 'ESTORNADA')");
                    table.ForeignKey(
                        name: "FK_faturas_assinatura_assinaturas_assinatura_id",
                        column: x => x.assinatura_id,
                        principalTable: "assinaturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_faturas_assinatura_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tenants_situacao_financeira",
                table: "tenants",
                sql: "situacao_financeira IN ('REGULAR', 'EM_ATRASO', 'SOMENTE_LEITURA')");

            migrationBuilder.CreateIndex(
                name: "IX_assinaturas_plano_id",
                table: "assinaturas",
                column: "plano_id");

            migrationBuilder.CreateIndex(
                name: "IX_assinaturas_tenant",
                table: "assinaturas",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_faturas_assinatura_asaas_cobranca",
                table: "faturas_assinatura",
                column: "asaas_cobranca_id");

            migrationBuilder.CreateIndex(
                name: "IX_faturas_assinatura_competencia",
                table: "faturas_assinatura",
                columns: new[] { "assinatura_id", "competencia" },
                unique: true,
                filter: "situacao <> 'CANCELADA'");

            migrationBuilder.CreateIndex(
                name: "IX_faturas_assinatura_nota_fiscal",
                table: "faturas_assinatura",
                column: "nota_fiscal_id");

            migrationBuilder.CreateIndex(
                name: "IX_faturas_assinatura_tenant_id",
                table: "faturas_assinatura",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_planos_assinatura_nome",
                table: "planos_assinatura",
                column: "nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "faturas_assinatura");

            migrationBuilder.DropTable(
                name: "assinaturas");

            migrationBuilder.DropTable(
                name: "planos_assinatura");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tenants_situacao_financeira",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "situacao_financeira",
                table: "tenants");
        }
    }
}
