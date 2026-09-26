using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChamadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chamadas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    turma_id = table.Column<int>(type: "integer", nullable: false),
                    disciplina_id = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    quantidade_aulas = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    conteudo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    registrado_por_usuario_id = table.Column<int>(type: "integer", nullable: false),
                    atualizado_por_usuario_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chamadas", x => x.id);
                    table.CheckConstraint("CK_chamadas_quantidade_aulas", "quantidade_aulas BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_chamadas_disciplinas_disciplina_id",
                        column: x => x.disciplina_id,
                        principalTable: "disciplinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamadas_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamadas_turmas_turma_id",
                        column: x => x.turma_id,
                        principalTable: "turmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamadas_users_atualizado_por_usuario_id",
                        column: x => x.atualizado_por_usuario_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamadas_users_registrado_por_usuario_id",
                        column: x => x.registrado_por_usuario_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chamadas_alunos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    chamada_id = table.Column<int>(type: "integer", nullable: false),
                    aluno_id = table.Column<int>(type: "integer", nullable: false),
                    situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    justificativa = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chamadas_alunos", x => x.id);
                    table.CheckConstraint("CK_chamadas_alunos_situacao", "situacao IN ('PRESENTE', 'FALTA', 'FALTA_JUSTIFICADA')");
                    table.ForeignKey(
                        name: "FK_chamadas_alunos_alunos_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "alunos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamadas_alunos_chamadas_chamada_id",
                        column: x => x.chamada_id,
                        principalTable: "chamadas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chamadas_alunos_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chamadas_atualizado_por_usuario_id",
                table: "chamadas",
                column: "atualizado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_chamadas_disciplina_id",
                table: "chamadas",
                column: "disciplina_id");

            migrationBuilder.CreateIndex(
                name: "IX_chamadas_registrado_por_usuario_id",
                table: "chamadas",
                column: "registrado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_chamadas_turma_disciplina_data",
                table: "chamadas",
                columns: new[] { "tenant_id", "turma_id", "disciplina_id", "data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chamadas_turma_id",
                table: "chamadas",
                column: "turma_id");

            migrationBuilder.CreateIndex(
                name: "IX_chamadas_alunos_aluno",
                table: "chamadas_alunos",
                column: "aluno_id");

            migrationBuilder.CreateIndex(
                name: "IX_chamadas_alunos_chamada_aluno",
                table: "chamadas_alunos",
                columns: new[] { "chamada_id", "aluno_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chamadas_alunos_tenant_id",
                table: "chamadas_alunos",
                column: "tenant_id");

            // Instituições que já têm matriz de permissões recebem o módulo novo com o mesmo padrão
            // de Permissoes.PadraoDoPerfil. As que ainda não têm recebem a matriz completa no startup.
            migrationBuilder.Sql("""
                INSERT INTO perfis_permissoes (tenant_id, perfil_id, permissao)
                SELECT t.id, p.id, x.permissao
                FROM tenants t
                JOIN perfis p ON TRUE
                JOIN (VALUES
                    ('gerência', 'chamada.visualizar'), ('gerência', 'chamada.criar'),
                    ('gerência', 'chamada.editar'), ('gerência', 'chamada.excluir'),
                    ('diretor', 'chamada.visualizar'), ('diretor', 'chamada.criar'),
                    ('diretor', 'chamada.editar'), ('diretor', 'chamada.excluir'),
                    ('secretário', 'chamada.visualizar'), ('secretário', 'chamada.criar'),
                    ('secretário', 'chamada.editar'),
                    ('professor', 'chamada.visualizar'), ('professor', 'chamada.criar'),
                    ('professor', 'chamada.editar')
                ) AS x(perfil, permissao) ON LOWER(p.nome) = x.perfil
                WHERE EXISTS (SELECT 1 FROM perfis_permissoes pp WHERE pp.tenant_id = t.id)
                ON CONFLICT (tenant_id, perfil_id, permissao) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM perfis_permissoes WHERE permissao LIKE 'chamada.%';");

            migrationBuilder.DropTable(
                name: "chamadas_alunos");

            migrationBuilder.DropTable(
                name: "chamadas");
        }
    }
}
