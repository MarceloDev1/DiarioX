using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnoLetivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "anos_letivos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ano_referencia = table.Column<int>(type: "integer", nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    data_termino = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo_periodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anos_letivos", x => x.id);
                    table.CheckConstraint("CK_anos_letivos_tipo_periodo", "tipo_periodo IN ('BIMESTRAL', 'TRIMESTRAL', 'SEMESTRAL')");
                });

            migrationBuilder.CreateTable(
                name: "periodos_avaliativos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ano_letivo_id = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    data_termino = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periodos_avaliativos", x => x.id);
                    table.ForeignKey(
                        name: "FK_periodos_avaliativos_anos_letivos_ano_letivo_id",
                        column: x => x.ano_letivo_id,
                        principalTable: "anos_letivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anos_letivos_ano_referencia",
                table: "anos_letivos",
                column: "ano_referencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_periodos_avaliativos_ano_numero",
                table: "periodos_avaliativos",
                columns: new[] { "ano_letivo_id", "numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "periodos_avaliativos");

            migrationBuilder.DropTable(
                name: "anos_letivos");
        }
    }
}
