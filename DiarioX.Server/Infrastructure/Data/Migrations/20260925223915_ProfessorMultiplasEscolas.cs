using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfessorMultiplasEscolas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "professor_escolas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    ProfessorId = table.Column<int>(type: "integer", nullable: false),
                    EscolaId = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professor_escolas", x => x.id);
                    table.ForeignKey(
                        name: "FK_professor_escolas_escolas",
                        column: x => x.EscolaId,
                        principalTable: "escolas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_professor_escolas_professores",
                        column: x => x.ProfessorId,
                        principalTable: "professores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_professor_escolas_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_professor_escolas_escola_id",
                table: "professor_escolas",
                column: "EscolaId");

            migrationBuilder.CreateIndex(
                name: "IX_professor_escolas_professor_id",
                table: "professor_escolas",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_professor_escolas_tenant_id",
                table: "professor_escolas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_professor_escolas_unique",
                table: "professor_escolas",
                columns: new[] { "ProfessorId", "EscolaId" },
                unique: true);

            // Preserva o vínculo atual de cada professor antes de remover a coluna
            migrationBuilder.Sql(
                """
                INSERT INTO professor_escolas (tenant_id, "ProfessorId", "EscolaId", created_at)
                SELECT tenant_id, id, "EscolaId", NOW()
                FROM professores;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_professores_escolas",
                table: "professores");

            migrationBuilder.DropIndex(
                name: "IX_professores_escola_id",
                table: "professores");

            migrationBuilder.DropColumn(
                name: "EscolaId",
                table: "professores");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EscolaId",
                table: "professores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Mantém a primeira escola vinculada como escola única do professor
            migrationBuilder.Sql(
                """
                UPDATE professores p
                SET "EscolaId" = pe."EscolaId"
                FROM (
                    SELECT DISTINCT ON ("ProfessorId") "ProfessorId", "EscolaId"
                    FROM professor_escolas
                    ORDER BY "ProfessorId", id
                ) pe
                WHERE pe."ProfessorId" = p.id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_professores_escola_id",
                table: "professores",
                column: "EscolaId");

            migrationBuilder.AddForeignKey(
                name: "FK_professores_escolas",
                table: "professores",
                column: "EscolaId",
                principalTable: "escolas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropTable(
                name: "professor_escolas");
        }
    }
}
