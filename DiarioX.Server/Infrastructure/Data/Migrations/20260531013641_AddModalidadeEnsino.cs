using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModalidadeEnsino : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "modalidades_ensino",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sigla = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    codigo_mec = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ATIVO")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modalidades_ensino", x => x.id);
                    table.CheckConstraint("CK_modalidades_ensino_status", "status IN ('ATIVO', 'INATIVO')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_modalidades_ensino_nome",
                table: "modalidades_ensino",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_modalidades_ensino_sigla",
                table: "modalidades_ensino",
                column: "sigla",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "modalidades_ensino");
        }
    }
}
