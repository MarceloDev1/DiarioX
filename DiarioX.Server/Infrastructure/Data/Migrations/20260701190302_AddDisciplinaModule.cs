using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDisciplinaModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "disciplinas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disciplinas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "disciplinas_etapas_ensino",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    disciplina_id = table.Column<int>(type: "integer", nullable: false),
                    etapa_ensino_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disciplinas_etapas_ensino", x => x.id);
                    table.ForeignKey(
                        name: "FK_disciplinas_etapas_ensino_disciplinas_disciplina_id",
                        column: x => x.disciplina_id,
                        principalTable: "disciplinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_disciplinas_etapas_ensino_etapas_ensino_etapa_ensino_id",
                        column: x => x.etapa_ensino_id,
                        principalTable: "etapas_ensino",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_disciplinas_codigo",
                table: "disciplinas",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_disciplinas_nome",
                table: "disciplinas",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "IX_disciplinas_etapas_ensino_etapa_ensino_id",
                table: "disciplinas_etapas_ensino",
                column: "etapa_ensino_id");

            migrationBuilder.CreateIndex(
                name: "IX_disciplinas_etapas_ensino_unicidade",
                table: "disciplinas_etapas_ensino",
                columns: new[] { "disciplina_id", "etapa_ensino_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "disciplinas_etapas_ensino");

            migrationBuilder.DropTable(
                name: "disciplinas");
        }
    }
}
