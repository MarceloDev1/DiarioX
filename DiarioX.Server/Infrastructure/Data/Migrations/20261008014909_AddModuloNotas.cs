using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloNotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "regra_avaliacao_id",
                table: "etapas_ensino",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "avaliacoes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    turma_id = table.Column<int>(type: "integer", nullable: false),
                    disciplina_id = table.Column<int>(type: "integer", nullable: false),
                    periodo_avaliativo_id = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: true),
                    peso = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false, defaultValue: 1m),
                    valor_maximo = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    registrado_por_usuario_id = table.Column<int>(type: "integer", nullable: false),
                    atualizado_por_usuario_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avaliacoes", x => x.id);
                    table.CheckConstraint("CK_avaliacoes_peso", "peso > 0");
                    table.CheckConstraint("CK_avaliacoes_tipo", "tipo IN ('PROVA', 'TRABALHO', 'ATIVIDADE', 'PARTICIPACAO', 'OUTRO', 'RECUPERACAO')");
                    table.CheckConstraint("CK_avaliacoes_valor_maximo", "valor_maximo IS NULL OR valor_maximo > 0");
                    table.ForeignKey(
                        name: "FK_avaliacoes_disciplinas_disciplina_id",
                        column: x => x.disciplina_id,
                        principalTable: "disciplinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_avaliacoes_periodos_avaliativos_periodo_avaliativo_id",
                        column: x => x.periodo_avaliativo_id,
                        principalTable: "periodos_avaliativos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_avaliacoes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_avaliacoes_turmas_turma_id",
                        column: x => x.turma_id,
                        principalTable: "turmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_avaliacoes_users_atualizado_por_usuario_id",
                        column: x => x.atualizado_por_usuario_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_avaliacoes_users_registrado_por_usuario_id",
                        column: x => x.registrado_por_usuario_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "regras_avaliacao",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nota_maxima = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    media_aprovacao = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    casas_decimais = table.Column<int>(type: "integer", nullable: false),
                    calculo_nota_periodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    permite_recuperacao = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regras_avaliacao", x => x.id);
                    table.CheckConstraint("CK_regras_avaliacao_calculo", "calculo_nota_periodo IN ('MEDIA_PONDERADA', 'SOMA')");
                    table.CheckConstraint("CK_regras_avaliacao_casas", "casas_decimais BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_regras_avaliacao_media", "media_aprovacao > 0 AND media_aprovacao <= nota_maxima");
                    table.ForeignKey(
                        name: "FK_regras_avaliacao_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notas_avaliacoes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    avaliacao_id = table.Column<int>(type: "integer", nullable: false),
                    aluno_id = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    lancada_por_usuario_id = table.Column<int>(type: "integer", nullable: false),
                    lancada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_avaliacoes", x => x.id);
                    table.CheckConstraint("CK_notas_avaliacoes_valor", "valor >= 0");
                    table.ForeignKey(
                        name: "FK_notas_avaliacoes_alunos_aluno_id",
                        column: x => x.aluno_id,
                        principalTable: "alunos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_avaliacoes_avaliacoes_avaliacao_id",
                        column: x => x.avaliacao_id,
                        principalTable: "avaliacoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notas_avaliacoes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_avaliacoes_users_lancada_por_usuario_id",
                        column: x => x.lancada_por_usuario_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_etapas_ensino_regra_avaliacao_id",
                table: "etapas_ensino",
                column: "regra_avaliacao_id");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_atualizado_por_usuario_id",
                table: "avaliacoes",
                column: "atualizado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_disciplina_id",
                table: "avaliacoes",
                column: "disciplina_id");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_periodo_avaliativo_id",
                table: "avaliacoes",
                column: "periodo_avaliativo_id");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_recuperacao_unica",
                table: "avaliacoes",
                columns: new[] { "turma_id", "disciplina_id", "periodo_avaliativo_id" },
                unique: true,
                filter: "tipo = 'RECUPERACAO'");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_registrado_por_usuario_id",
                table: "avaliacoes",
                column: "registrado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacoes_turma_disciplina_periodo",
                table: "avaliacoes",
                columns: new[] { "tenant_id", "turma_id", "disciplina_id", "periodo_avaliativo_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_avaliacoes_aluno",
                table: "notas_avaliacoes",
                column: "aluno_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_avaliacoes_avaliacao_aluno",
                table: "notas_avaliacoes",
                columns: new[] { "avaliacao_id", "aluno_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notas_avaliacoes_lancada_por_usuario_id",
                table: "notas_avaliacoes",
                column: "lancada_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_avaliacoes_tenant_id",
                table: "notas_avaliacoes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_regras_avaliacao_nome",
                table: "regras_avaliacao",
                columns: new[] { "tenant_id", "nome" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_etapas_ensino_regras_avaliacao_regra_avaliacao_id",
                table: "etapas_ensino",
                column: "regra_avaliacao_id",
                principalTable: "regras_avaliacao",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);


            // Instituições que já têm matriz de permissões recebem os módulos novos com o mesmo padrão
            // de Permissoes.PadraoDoPerfil. As que ainda não têm recebem a matriz completa no startup.
            migrationBuilder.Sql("""
                INSERT INTO perfis_permissoes (tenant_id, perfil_id, permissao)
                SELECT t.id, p.id, x.permissao
                FROM tenants t
                JOIN (VALUES
                    ('gerência', 'notas.visualizar'), ('gerência', 'notas.criar'), ('gerência', 'notas.editar'), ('gerência', 'notas.excluir'),
                    ('gerência', 'regras-avaliacao.visualizar'), ('gerência', 'regras-avaliacao.criar'),
                    ('gerência', 'regras-avaliacao.editar'), ('gerência', 'regras-avaliacao.excluir'),
                    ('diretor', 'notas.visualizar'), ('diretor', 'notas.criar'), ('diretor', 'notas.editar'), ('diretor', 'notas.excluir'),
                    ('diretor', 'regras-avaliacao.visualizar'), ('diretor', 'regras-avaliacao.criar'),
                    ('diretor', 'regras-avaliacao.editar'), ('diretor', 'regras-avaliacao.excluir'),
                    ('secretário', 'notas.visualizar'), ('secretário', 'notas.criar'), ('secretário', 'notas.editar'),
                    ('secretário', 'regras-avaliacao.visualizar'),
                    ('professor', 'notas.visualizar'), ('professor', 'notas.criar'), ('professor', 'notas.editar'), ('professor', 'notas.excluir')
                ) AS x(perfil, permissao) ON TRUE
                JOIN perfis p ON LOWER(p.nome) = x.perfil
                WHERE EXISTS (SELECT 1 FROM perfis_permissoes pp WHERE pp.tenant_id = t.id)
                ON CONFLICT (tenant_id, perfil_id, permissao) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM perfis_permissoes WHERE permissao LIKE 'notas.%' OR permissao LIKE 'regras-avaliacao.%';");

            migrationBuilder.DropForeignKey(
                name: "FK_etapas_ensino_regras_avaliacao_regra_avaliacao_id",
                table: "etapas_ensino");

            migrationBuilder.DropTable(
                name: "notas_avaliacoes");

            migrationBuilder.DropTable(
                name: "regras_avaliacao");

            migrationBuilder.DropTable(
                name: "avaliacoes");

            migrationBuilder.DropIndex(
                name: "IX_etapas_ensino_regra_avaliacao_id",
                table: "etapas_ensino");

            migrationBuilder.DropColumn(
                name: "regra_avaliacao_id",
                table: "etapas_ensino");
        }
    }
}
