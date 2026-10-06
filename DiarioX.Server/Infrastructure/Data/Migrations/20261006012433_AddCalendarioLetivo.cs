using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarioLetivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "calendarios_letivos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    ano_letivo_id = table.Column<int>(type: "integer", nullable: false),
                    escola_id = table.Column<int>(type: "integer", nullable: true),
                    publicado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    publicado_por_usuario_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calendarios_letivos", x => x.id);
                    table.ForeignKey(
                        name: "FK_calendarios_letivos_anos_letivos_ano_letivo_id",
                        column: x => x.ano_letivo_id,
                        principalTable: "anos_letivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_calendarios_letivos_escolas_escola_id",
                        column: x => x.escola_id,
                        principalTable: "escolas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_calendarios_letivos_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_calendarios_letivos_users_publicado_por_usuario_id",
                        column: x => x.publicado_por_usuario_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eventos_calendario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    calendario_letivo_id = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    com_aula = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_calendario", x => x.id);
                    table.CheckConstraint("CK_eventos_calendario_tipo", "tipo IN ('FERIADO', 'RECESSO', 'PONTO_FACULTATIVO', 'CONSELHO_CLASSE', 'PLANTAO_PEDAGOGICO', 'FORMACAO_CONTINUADA', 'SABADO_LETIVO')");
                    table.ForeignKey(
                        name: "FK_eventos_calendario_calendarios_letivos_calendario_letivo_id",
                        column: x => x.calendario_letivo_id,
                        principalTable: "calendarios_letivos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_eventos_calendario_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_calendarios_letivos_ano_escola",
                table: "calendarios_letivos",
                columns: new[] { "ano_letivo_id", "escola_id" },
                unique: true,
                filter: "escola_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_calendarios_letivos_ano_rede",
                table: "calendarios_letivos",
                column: "ano_letivo_id",
                unique: true,
                filter: "escola_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_calendarios_letivos_escola_id",
                table: "calendarios_letivos",
                column: "escola_id");

            migrationBuilder.CreateIndex(
                name: "IX_calendarios_letivos_publicado_por_usuario_id",
                table: "calendarios_letivos",
                column: "publicado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_calendarios_letivos_tenant_id",
                table: "calendarios_letivos",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_calendario_calendario_data",
                table: "eventos_calendario",
                columns: new[] { "calendario_letivo_id", "data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_eventos_calendario_tenant_id",
                table: "eventos_calendario",
                column: "tenant_id");

            // Instituições que já têm matriz de permissões recebem o módulo novo com o mesmo padrão
            // de Permissoes.PadraoDoPerfil. As que ainda não têm recebem a matriz completa no startup.
            migrationBuilder.Sql("""
                INSERT INTO perfis_permissoes (tenant_id, perfil_id, permissao)
                SELECT t.id, p.id, x.permissao
                FROM tenants t
                JOIN perfis p ON LOWER(p.nome) IN ('gerência', 'diretor', 'secretário', 'professor')
                JOIN (VALUES ('calendario-letivo.visualizar'), ('calendario-letivo.editar')) AS x(permissao)
                  ON x.permissao = 'calendario-letivo.visualizar' OR LOWER(p.nome) IN ('gerência', 'diretor')
                WHERE EXISTS (SELECT 1 FROM perfis_permissoes pp WHERE pp.tenant_id = t.id)
                ON CONFLICT (tenant_id, perfil_id, permissao) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM perfis_permissoes WHERE permissao LIKE 'calendario-letivo.%';");

            migrationBuilder.DropTable(
                name: "eventos_calendario");

            migrationBuilder.DropTable(
                name: "calendarios_letivos");
        }
    }
}
