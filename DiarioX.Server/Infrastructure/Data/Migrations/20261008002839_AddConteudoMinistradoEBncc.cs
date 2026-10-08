using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConteudoMinistradoEBncc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conteudos_ministrados",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    turma_id = table.Column<int>(type: "integer", nullable: false),
                    disciplina_id = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    registrado_por_usuario_id = table.Column<int>(type: "integer", nullable: false),
                    atualizado_por_usuario_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conteudos_ministrados", x => x.id);
                    table.ForeignKey(
                        name: "FK_conteudos_ministrados_disciplinas_disciplina_id",
                        column: x => x.disciplina_id,
                        principalTable: "disciplinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conteudos_ministrados_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conteudos_ministrados_turmas_turma_id",
                        column: x => x.turma_id,
                        principalTable: "turmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conteudos_ministrados_users_atualizado_por_usuario_id",
                        column: x => x.atualizado_por_usuario_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conteudos_ministrados_users_registrado_por_usuario_id",
                        column: x => x.registrado_por_usuario_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "habilidades_bncc",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    disciplina_id = table.Column<int>(type: "integer", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_habilidades_bncc", x => x.id);
                    table.ForeignKey(
                        name: "FK_habilidades_bncc_disciplinas_disciplina_id",
                        column: x => x.disciplina_id,
                        principalTable: "disciplinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_habilidades_bncc_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conteudos_ministrados_habilidades",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    conteudo_ministrado_id = table.Column<int>(type: "integer", nullable: false),
                    habilidade_bncc_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conteudos_ministrados_habilidades", x => x.id);
                    table.ForeignKey(
                        name: "FK_conteudos_ministrados_habilidades_conteudos_ministrados_con~",
                        column: x => x.conteudo_ministrado_id,
                        principalTable: "conteudos_ministrados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_conteudos_ministrados_habilidades_habilidades_bncc_habilida~",
                        column: x => x.habilidade_bncc_id,
                        principalTable: "habilidades_bncc",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conteudos_ministrados_habilidades_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "habilidades_bncc_etapas_ensino",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    habilidade_bncc_id = table.Column<int>(type: "integer", nullable: false),
                    etapa_ensino_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_habilidades_bncc_etapas_ensino", x => x.id);
                    table.ForeignKey(
                        name: "FK_habilidades_bncc_etapas_ensino_etapas_ensino_etapa_ensino_id",
                        column: x => x.etapa_ensino_id,
                        principalTable: "etapas_ensino",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_habilidades_bncc_etapas_ensino_habilidades_bncc_habilidade_~",
                        column: x => x.habilidade_bncc_id,
                        principalTable: "habilidades_bncc",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_habilidades_bncc_etapas_ensino_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conteudos_ministrados_atualizado_por_usuario_id",
                table: "conteudos_ministrados",
                column: "atualizado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_conteudos_ministrados_disciplina_id",
                table: "conteudos_ministrados",
                column: "disciplina_id");

            migrationBuilder.CreateIndex(
                name: "IX_conteudos_ministrados_registrado_por_usuario_id",
                table: "conteudos_ministrados",
                column: "registrado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_conteudos_ministrados_turma_disciplina_data",
                table: "conteudos_ministrados",
                columns: new[] { "tenant_id", "turma_id", "disciplina_id", "data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conteudos_ministrados_turma_id",
                table: "conteudos_ministrados",
                column: "turma_id");

            migrationBuilder.CreateIndex(
                name: "IX_conteudos_ministrados_habilidades_habilidade",
                table: "conteudos_ministrados_habilidades",
                column: "habilidade_bncc_id");

            migrationBuilder.CreateIndex(
                name: "IX_conteudos_ministrados_habilidades_tenant_id",
                table: "conteudos_ministrados_habilidades",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_conteudos_ministrados_habilidades_unicidade",
                table: "conteudos_ministrados_habilidades",
                columns: new[] { "conteudo_ministrado_id", "habilidade_bncc_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_habilidades_bncc_codigo",
                table: "habilidades_bncc",
                columns: new[] { "tenant_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_habilidades_bncc_disciplina",
                table: "habilidades_bncc",
                column: "disciplina_id");

            migrationBuilder.CreateIndex(
                name: "IX_habilidades_bncc_etapas_ensino_etapa",
                table: "habilidades_bncc_etapas_ensino",
                column: "etapa_ensino_id");

            migrationBuilder.CreateIndex(
                name: "IX_habilidades_bncc_etapas_ensino_tenant_id",
                table: "habilidades_bncc_etapas_ensino",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_habilidades_bncc_etapas_ensino_unicidade",
                table: "habilidades_bncc_etapas_ensino",
                columns: new[] { "habilidade_bncc_id", "etapa_ensino_id" },
                unique: true);

            // O conteúdo da aula passa a ser um registro próprio (conteudos_ministrados). Leva o texto das chamadas
            // por disciplina; as chamadas diárias (sem disciplina) não têm a que disciplina atribuí-lo.
            migrationBuilder.Sql("""
                INSERT INTO conteudos_ministrados
                    (tenant_id, turma_id, disciplina_id, data, descricao, registrado_por_usuario_id, atualizado_por_usuario_id, created_at, updated_at)
                SELECT c.tenant_id, c.turma_id, c.disciplina_id, c.data, c.conteudo, c.registrado_por_usuario_id,
                       c.atualizado_por_usuario_id, c.created_at, c.updated_at
                FROM chamadas c
                WHERE c.disciplina_id IS NOT NULL AND c.conteudo IS NOT NULL AND BTRIM(c.conteudo) <> '';
                """);

            migrationBuilder.DropColumn(
                name: "conteudo",
                table: "chamadas");

            // Instituições que já têm matriz de permissões recebem os módulos novos com o mesmo padrão
            // de Permissoes.PadraoDoPerfil. As que ainda não têm recebem a matriz completa no startup.
            migrationBuilder.Sql("""
                INSERT INTO perfis_permissoes (tenant_id, perfil_id, permissao)
                SELECT t.id, p.id, x.permissao
                FROM tenants t
                JOIN perfis p ON LOWER(p.nome) IN ('gerência', 'diretor', 'secretário', 'professor')
                JOIN (VALUES
                    ('conteudo-ministrado.visualizar', 'gerência,diretor,secretário,professor'),
                    ('conteudo-ministrado.criar',      'gerência,diretor,secretário,professor'),
                    ('conteudo-ministrado.editar',     'gerência,diretor,secretário,professor'),
                    ('conteudo-ministrado.excluir',    'gerência,diretor,professor'),
                    ('habilidades-bncc.visualizar',    'gerência,diretor,secretário,professor'),
                    ('habilidades-bncc.criar',         'gerência,diretor'),
                    ('habilidades-bncc.editar',        'gerência,diretor'),
                    ('habilidades-bncc.excluir',       'gerência,diretor')
                ) AS x(permissao, perfis)
                  ON LOWER(p.nome) = ANY (string_to_array(x.perfis, ','))
                WHERE EXISTS (SELECT 1 FROM perfis_permissoes pp WHERE pp.tenant_id = t.id)
                ON CONFLICT (tenant_id, perfil_id, permissao) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM perfis_permissoes WHERE permissao LIKE 'conteudo-ministrado.%' OR permissao LIKE 'habilidades-bncc.%';");

            migrationBuilder.DropTable(
                name: "conteudos_ministrados_habilidades");

            migrationBuilder.DropTable(
                name: "habilidades_bncc_etapas_ensino");

            migrationBuilder.DropTable(
                name: "conteudos_ministrados");

            migrationBuilder.DropTable(
                name: "habilidades_bncc");

            migrationBuilder.AddColumn<string>(
                name: "conteudo",
                table: "chamadas",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
