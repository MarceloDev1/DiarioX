using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHorariosAula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "horarios_aula",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    turma_id = table.Column<int>(type: "integer", nullable: false),
                    dia_semana = table.Column<int>(type: "integer", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    disciplina_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_horarios_aula", x => x.id);
                    table.ForeignKey(
                        name: "FK_horarios_aula_disciplinas_disciplina_id",
                        column: x => x.disciplina_id,
                        principalTable: "disciplinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_horarios_aula_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_horarios_aula_turmas_turma_id",
                        column: x => x.turma_id,
                        principalTable: "turmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_horarios_aula_disciplina_id",
                table: "horarios_aula",
                column: "disciplina_id");

            migrationBuilder.CreateIndex(
                name: "IX_horarios_aula_turma_dia_ordem",
                table: "horarios_aula",
                columns: new[] { "tenant_id", "turma_id", "dia_semana", "ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_horarios_aula_turma_id",
                table: "horarios_aula",
                column: "turma_id");

            // Instituições que já têm matriz de permissões recebem o módulo novo com o mesmo padrão
            // de Permissoes.PadraoDoPerfil. As que ainda não têm recebem a matriz completa no startup.
            migrationBuilder.Sql("""
                INSERT INTO perfis_permissoes (tenant_id, perfil_id, permissao)
                SELECT t.id, p.id, x.permissao
                FROM tenants t
                JOIN perfis p ON LOWER(p.nome) IN ('gerência', 'diretor', 'secretário', 'professor')
                JOIN (VALUES
                    ('horarios.visualizar', 'gerência,diretor,secretário,professor'),
                    ('horarios.editar',     'gerência,diretor,secretário')
                ) AS x(permissao, perfis)
                  ON LOWER(p.nome) = ANY (string_to_array(x.perfis, ','))
                WHERE EXISTS (SELECT 1 FROM perfis_permissoes pp WHERE pp.tenant_id = t.id)
                ON CONFLICT (tenant_id, perfil_id, permissao) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM perfis_permissoes WHERE permissao LIKE 'horarios.%';");

            migrationBuilder.DropTable(
                name: "horarios_aula");
        }
    }
}
