using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEtapaEnsino : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "etapas_ensino",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    modalidade_ensino_id = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sigla = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ordem_cronologica = table.Column<int>(type: "integer", nullable: false),
                    idade_recomendada = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etapas_ensino", x => x.id);
                    table.ForeignKey(
                        name: "FK_etapas_ensino_modalidades_ensino_modalidade_ensino_id",
                        column: x => x.modalidade_ensino_id,
                        principalTable: "modalidades_ensino",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_etapas_ensino_modalidade_nome",
                table: "etapas_ensino",
                columns: new[] { "modalidade_ensino_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_etapas_ensino_modalidade_ordem",
                table: "etapas_ensino",
                columns: new[] { "modalidade_ensino_id", "ordem_cronologica" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_etapas_ensino_sigla",
                table: "etapas_ensino",
                column: "sigla",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "etapas_ensino");
        }
    }
}
