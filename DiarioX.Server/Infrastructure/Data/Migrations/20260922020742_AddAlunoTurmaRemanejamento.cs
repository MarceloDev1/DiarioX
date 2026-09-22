using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlunoTurmaRemanejamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alunos_turmas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    aluno_id = table.Column<int>(type: "integer", nullable: false),
                    turma_id = table.Column<int>(type: "integer", nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    data_fim = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alunos_turmas", x => x.id);
                    table.CheckConstraint("CK_alunos_turmas_periodo", "data_fim IS NULL OR data_fim >= data_inicio");
                    table.ForeignKey(
                        name: "FK_alunos_turmas_alunos_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "alunos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alunos_turmas_turmas_turma_id",
                        column: x => x.turma_id,
                        principalTable: "turmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alunos_turmas_aluno_ativo",
                table: "alunos_turmas",
                column: "aluno_id",
                unique: true,
                filter: "data_fim IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_alunos_turmas_aluno_inicio",
                table: "alunos_turmas",
                columns: new[] { "aluno_id", "data_inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_alunos_turmas_turma_periodo",
                table: "alunos_turmas",
                columns: new[] { "turma_id", "data_inicio", "data_fim" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alunos_turmas");
        }
    }
}
