using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTurma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "turmas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ano_letivo_id = table.Column<int>(type: "integer", nullable: false),
                    escola_id = table.Column<int>(type: "integer", nullable: false),
                    modalidade_ensino_id = table.Column<int>(type: "integer", nullable: false),
                    etapa_ensino_id = table.Column<int>(type: "integer", nullable: false),
                    nome_identificador = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nome_completo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    turno = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    vagas_ofertadas = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ATIVO")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_turmas", x => x.id);
                    table.CheckConstraint("CK_turmas_status", "status IN ('ATIVO', 'INATIVO')");
                    table.CheckConstraint("CK_turmas_turno", "turno IN ('MANHA', 'TARDE', 'NOITE', 'INTEGRAL')");
                    table.CheckConstraint("CK_turmas_vagas_ofertadas", "vagas_ofertadas > 0");
                    table.ForeignKey(
                        name: "FK_turmas_anos_letivos_ano_letivo_id",
                        column: x => x.ano_letivo_id,
                        principalTable: "anos_letivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_turmas_escolas_escola_id",
                        column: x => x.escola_id,
                        principalTable: "escolas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_turmas_etapas_ensino_etapa_ensino_id",
                        column: x => x.etapa_ensino_id,
                        principalTable: "etapas_ensino",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_turmas_modalidades_ensino_modalidade_ensino_id",
                        column: x => x.modalidade_ensino_id,
                        principalTable: "modalidades_ensino",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_turmas_escola_id",
                table: "turmas",
                column: "escola_id");

            migrationBuilder.CreateIndex(
                name: "IX_turmas_etapa_ensino_id",
                table: "turmas",
                column: "etapa_ensino_id");

            migrationBuilder.CreateIndex(
                name: "IX_turmas_modalidade_ensino_id",
                table: "turmas",
                column: "modalidade_ensino_id");

            migrationBuilder.CreateIndex(
                name: "IX_turmas_unicidade",
                table: "turmas",
                columns: new[] { "ano_letivo_id", "escola_id", "etapa_ensino_id", "nome_identificador", "turno" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "turmas");
        }
    }
}
