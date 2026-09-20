using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "professores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    EscolaId = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    cpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    data_nascimento = table.Column<DateTime>(type: "date", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    matricula = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data_admissao = table.Column<DateTime>(type: "date", nullable: false),
                    situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ATIVO"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professores", x => x.id);
                    table.CheckConstraint("CK_professores_situacao", "situacao IN ('ATIVO', 'INATIVO', 'AFASTADO', 'LICENCIADO')");
                    table.ForeignKey(
                        name: "FK_professores_escolas",
                        column: x => x.EscolaId,
                        principalTable: "escolas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_professores_usuarios",
                        column: x => x.UsuarioId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "professor_disciplinas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfessorId = table.Column<int>(type: "integer", nullable: false),
                    DisciplinaId = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professor_disciplinas", x => x.id);
                    table.ForeignKey(
                        name: "FK_professor_disciplinas_disciplinas",
                        column: x => x.DisciplinaId,
                        principalTable: "disciplinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_professor_disciplinas_professores",
                        column: x => x.ProfessorId,
                        principalTable: "professores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_professor_disciplinas_disciplina_id",
                table: "professor_disciplinas",
                column: "DisciplinaId");

            migrationBuilder.CreateIndex(
                name: "IX_professor_disciplinas_professor_id",
                table: "professor_disciplinas",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_professor_disciplinas_unique",
                table: "professor_disciplinas",
                columns: new[] { "ProfessorId", "DisciplinaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_professores_cpf",
                table: "professores",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_professores_email",
                table: "professores",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_professores_escola_id",
                table: "professores",
                column: "EscolaId");

            migrationBuilder.CreateIndex(
                name: "IX_professores_matricula",
                table: "professores",
                column: "matricula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_professores_UsuarioId",
                table: "professores",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "professor_disciplinas");

            migrationBuilder.DropTable(
                name: "professores");
        }
    }
}
