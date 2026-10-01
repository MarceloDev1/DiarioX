using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_alunos_turmas_motivo_desenturmacao",
                table: "alunos_turmas");

            migrationBuilder.CreateTable(
                name: "transferencias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    aluno_id = table.Column<int>(type: "integer", nullable: false),
                    escola_origem_id = table.Column<int>(type: "integer", nullable: false),
                    turma_id = table.Column<int>(type: "integer", nullable: true),
                    ano_letivo_id = table.Column<int>(type: "integer", nullable: false),
                    data_transferencia = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    escola_destino = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    registrado_por_usuario_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencias", x => x.id);
                    table.CheckConstraint("CK_transferencias_tipo", "tipo IN ('OUTRA_REDE', 'ENTRE_ESCOLAS_DA_REDE', 'MUDANCA_MUNICIPIO_ESTADO')");
                    table.ForeignKey(
                        name: "FK_transferencias_alunos_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "alunos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_anos_letivos_ano_letivo_id",
                        column: x => x.ano_letivo_id,
                        principalTable: "anos_letivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_escolas_escola_origem_id",
                        column: x => x.escola_origem_id,
                        principalTable: "escolas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_turmas_turma_id",
                        column: x => x.turma_id,
                        principalTable: "turmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_alunos_turmas_motivo_desenturmacao",
                table: "alunos_turmas",
                sql: "motivo_desenturmacao IS NULL OR motivo_desenturmacao IN ('REESTRUTURACAO_INTERNA', 'NAO_COMPARECEU', 'FALECIMENTO', 'ERRO_MATRICULA_ENTURMACAO', 'OUTROS', 'TRANSFERENCIA')");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_aluno_data",
                table: "transferencias",
                columns: new[] { "aluno_id", "data_transferencia" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_ano_letivo_id",
                table: "transferencias",
                column: "ano_letivo_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_escola_origem_id",
                table: "transferencias",
                column: "escola_origem_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_tenant_id",
                table: "transferencias",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_turma_id",
                table: "transferencias",
                column: "turma_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transferencias");

            migrationBuilder.DropCheckConstraint(
                name: "CK_alunos_turmas_motivo_desenturmacao",
                table: "alunos_turmas");

            // Vínculos encerrados por transferência não cabem na lista antiga de motivos.
            migrationBuilder.Sql("UPDATE alunos_turmas SET motivo_desenturmacao = NULL WHERE motivo_desenturmacao = 'TRANSFERENCIA';");

            migrationBuilder.AddCheckConstraint(
                name: "CK_alunos_turmas_motivo_desenturmacao",
                table: "alunos_turmas",
                sql: "motivo_desenturmacao IS NULL OR motivo_desenturmacao IN ('REESTRUTURACAO_INTERNA', 'NAO_COMPARECEU', 'FALECIMENTO', 'ERRO_MATRICULA_ENTURMACAO', 'OUTROS')");
        }
    }
}
