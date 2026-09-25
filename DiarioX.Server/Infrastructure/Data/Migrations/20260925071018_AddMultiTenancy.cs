using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        private const string DefaultTenantNome = "Instituição Padrão";
        private const string DefaultTenantSlug = "padrao";

        // Tabelas das entidades ITenantEntity (tenant_id obrigatório).
        private static readonly string[] TenantTables =
        {
            "escolas",
            "anos_letivos",
            "periodos_avaliativos",
            "modalidades_ensino",
            "etapas_ensino",
            "disciplinas",
            "disciplinas_etapas_ensino",
            "professores",
            "professor_disciplinas",
            "professor_alocacoes",
            "turmas",
            "alunos",
            "alunos_turmas",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_cpf",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_professores_cpf",
                table: "professores");

            migrationBuilder.DropIndex(
                name: "IX_professores_email",
                table: "professores");

            migrationBuilder.DropIndex(
                name: "IX_professores_matricula",
                table: "professores");

            migrationBuilder.DropIndex(
                name: "IX_modalidades_ensino_nome",
                table: "modalidades_ensino");

            migrationBuilder.DropIndex(
                name: "IX_modalidades_ensino_sigla",
                table: "modalidades_ensino");

            migrationBuilder.DropIndex(
                name: "IX_etapas_ensino_sigla",
                table: "etapas_ensino");

            migrationBuilder.DropIndex(
                name: "IX_escolas_codigo_inep",
                table: "escolas");

            migrationBuilder.DropIndex(
                name: "IX_disciplinas_codigo",
                table: "disciplinas");

            migrationBuilder.DropIndex(
                name: "IX_anos_letivos_ano_referencia",
                table: "anos_letivos");

            migrationBuilder.DropIndex(
                name: "IX_alunos_cpf_aluno",
                table: "alunos");

            migrationBuilder.DropIndex(
                name: "IX_alunos_matricula",
                table: "alunos");

            migrationBuilder.DropIndex(
                name: "IX_alunos_nome_nascimento_responsavel",
                table: "alunos");

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ATIVO"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                    table.CheckConstraint("CK_tenants_status", "status IN ('ATIVO', 'INATIVO')");
                });

            // Os dados existentes passam a pertencer a uma instituição padrão.
            migrationBuilder.Sql($"""
                INSERT INTO tenants (nome, slug, status)
                VALUES ('{DefaultTenantNome}', '{DefaultTenantSlug}', 'ATIVO');
                """);

            // A coluna nasce anulável, é preenchida com a instituição padrão e só então vira NOT NULL.
            foreach (var table in TenantTables)
            {
                migrationBuilder.AddColumn<int>(
                    name: "tenant_id",
                    table: table,
                    type: "integer",
                    nullable: true);

                migrationBuilder.Sql($"""
                    UPDATE {table}
                    SET tenant_id = (SELECT id FROM tenants WHERE slug = '{DefaultTenantSlug}');
                    """);

                migrationBuilder.AlterColumn<int>(
                    name: "tenant_id",
                    table: table,
                    type: "integer",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "integer",
                    oldNullable: true);
            }

            // Usuários com o perfil Administrador sem escola continuam globais (tenant_id nulo);
            // todos os demais passam a pertencer à instituição padrão.
            migrationBuilder.AddColumn<int>(
                name: "tenant_id",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql($"""
                UPDATE users
                SET tenant_id = (SELECT id FROM tenants WHERE slug = '{DefaultTenantSlug}')
                WHERE id NOT IN (
                    SELECT up.usuario_id
                    FROM usuarios_perfis up
                    JOIN perfis p ON p.id = up.perfil_id
                    WHERE LOWER(p.nome) = 'administrador' AND up.escola_id IS NULL);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_users_cpf_global",
                table: "users",
                column: "cpf",
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_email_global",
                table: "users",
                column: "email",
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_tenant_cpf",
                table: "users",
                columns: new[] { "tenant_id", "cpf" },
                unique: true,
                filter: "tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_tenant_email",
                table: "users",
                columns: new[] { "tenant_id", "email" },
                unique: true,
                filter: "tenant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_turmas_tenant_id",
                table: "turmas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_professores_cpf",
                table: "professores",
                columns: new[] { "tenant_id", "cpf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_professores_email",
                table: "professores",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_professores_matricula",
                table: "professores",
                columns: new[] { "tenant_id", "matricula" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_professor_disciplinas_tenant_id",
                table: "professor_disciplinas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_professor_alocacoes_tenant_id",
                table: "professor_alocacoes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_periodos_avaliativos_tenant_id",
                table: "periodos_avaliativos",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_modalidades_ensino_nome",
                table: "modalidades_ensino",
                columns: new[] { "tenant_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_modalidades_ensino_sigla",
                table: "modalidades_ensino",
                columns: new[] { "tenant_id", "sigla" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_etapas_ensino_sigla",
                table: "etapas_ensino",
                columns: new[] { "tenant_id", "sigla" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_escolas_codigo_inep",
                table: "escolas",
                columns: new[] { "tenant_id", "codigo_inep" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_disciplinas_etapas_ensino_tenant_id",
                table: "disciplinas_etapas_ensino",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_disciplinas_codigo",
                table: "disciplinas",
                columns: new[] { "tenant_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_anos_letivos_ano_referencia",
                table: "anos_letivos",
                columns: new[] { "tenant_id", "ano_referencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alunos_turmas_tenant_id",
                table: "alunos_turmas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_alunos_cpf_aluno",
                table: "alunos",
                columns: new[] { "tenant_id", "cpf_aluno" },
                unique: true,
                filter: "cpf_aluno IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_alunos_matricula",
                table: "alunos",
                columns: new[] { "tenant_id", "matricula" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alunos_nome_nascimento_responsavel",
                table: "alunos",
                columns: new[] { "tenant_id", "nome", "data_nascimento", "responsavel_nome1" },
                unique: true,
                filter: "cpf_aluno IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_slug",
                table: "tenants",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_alunos_tenants_tenant_id",
                table: "alunos",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_alunos_turmas_tenants_tenant_id",
                table: "alunos_turmas",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_anos_letivos_tenants_tenant_id",
                table: "anos_letivos",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_disciplinas_tenants_tenant_id",
                table: "disciplinas",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_disciplinas_etapas_ensino_tenants_tenant_id",
                table: "disciplinas_etapas_ensino",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_escolas_tenants_tenant_id",
                table: "escolas",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_etapas_ensino_tenants_tenant_id",
                table: "etapas_ensino",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_modalidades_ensino_tenants_tenant_id",
                table: "modalidades_ensino",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_periodos_avaliativos_tenants_tenant_id",
                table: "periodos_avaliativos",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_professor_alocacoes_tenants_tenant_id",
                table: "professor_alocacoes",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_professor_disciplinas_tenants_tenant_id",
                table: "professor_disciplinas",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_professores_tenants_tenant_id",
                table: "professores",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_turmas_tenants_tenant_id",
                table: "turmas",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_tenants_tenant_id",
                table: "users",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_alunos_tenants_tenant_id",
                table: "alunos");

            migrationBuilder.DropForeignKey(
                name: "FK_alunos_turmas_tenants_tenant_id",
                table: "alunos_turmas");

            migrationBuilder.DropForeignKey(
                name: "FK_anos_letivos_tenants_tenant_id",
                table: "anos_letivos");

            migrationBuilder.DropForeignKey(
                name: "FK_disciplinas_tenants_tenant_id",
                table: "disciplinas");

            migrationBuilder.DropForeignKey(
                name: "FK_disciplinas_etapas_ensino_tenants_tenant_id",
                table: "disciplinas_etapas_ensino");

            migrationBuilder.DropForeignKey(
                name: "FK_escolas_tenants_tenant_id",
                table: "escolas");

            migrationBuilder.DropForeignKey(
                name: "FK_etapas_ensino_tenants_tenant_id",
                table: "etapas_ensino");

            migrationBuilder.DropForeignKey(
                name: "FK_modalidades_ensino_tenants_tenant_id",
                table: "modalidades_ensino");

            migrationBuilder.DropForeignKey(
                name: "FK_periodos_avaliativos_tenants_tenant_id",
                table: "periodos_avaliativos");

            migrationBuilder.DropForeignKey(
                name: "FK_professor_alocacoes_tenants_tenant_id",
                table: "professor_alocacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_professor_disciplinas_tenants_tenant_id",
                table: "professor_disciplinas");

            migrationBuilder.DropForeignKey(
                name: "FK_professores_tenants_tenant_id",
                table: "professores");

            migrationBuilder.DropForeignKey(
                name: "FK_turmas_tenants_tenant_id",
                table: "turmas");

            migrationBuilder.DropForeignKey(
                name: "FK_users_tenants_tenant_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_users_cpf_global",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_email_global",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_tenant_cpf",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_tenant_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_turmas_tenant_id",
                table: "turmas");

            migrationBuilder.DropIndex(
                name: "IX_professores_cpf",
                table: "professores");

            migrationBuilder.DropIndex(
                name: "IX_professores_email",
                table: "professores");

            migrationBuilder.DropIndex(
                name: "IX_professores_matricula",
                table: "professores");

            migrationBuilder.DropIndex(
                name: "IX_professor_disciplinas_tenant_id",
                table: "professor_disciplinas");

            migrationBuilder.DropIndex(
                name: "IX_professor_alocacoes_tenant_id",
                table: "professor_alocacoes");

            migrationBuilder.DropIndex(
                name: "IX_periodos_avaliativos_tenant_id",
                table: "periodos_avaliativos");

            migrationBuilder.DropIndex(
                name: "IX_modalidades_ensino_nome",
                table: "modalidades_ensino");

            migrationBuilder.DropIndex(
                name: "IX_modalidades_ensino_sigla",
                table: "modalidades_ensino");

            migrationBuilder.DropIndex(
                name: "IX_etapas_ensino_sigla",
                table: "etapas_ensino");

            migrationBuilder.DropIndex(
                name: "IX_escolas_codigo_inep",
                table: "escolas");

            migrationBuilder.DropIndex(
                name: "IX_disciplinas_etapas_ensino_tenant_id",
                table: "disciplinas_etapas_ensino");

            migrationBuilder.DropIndex(
                name: "IX_disciplinas_codigo",
                table: "disciplinas");

            migrationBuilder.DropIndex(
                name: "IX_anos_letivos_ano_referencia",
                table: "anos_letivos");

            migrationBuilder.DropIndex(
                name: "IX_alunos_turmas_tenant_id",
                table: "alunos_turmas");

            migrationBuilder.DropIndex(
                name: "IX_alunos_cpf_aluno",
                table: "alunos");

            migrationBuilder.DropIndex(
                name: "IX_alunos_matricula",
                table: "alunos");

            migrationBuilder.DropIndex(
                name: "IX_alunos_nome_nascimento_responsavel",
                table: "alunos");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "turmas");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "professores");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "professor_disciplinas");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "professor_alocacoes");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "periodos_avaliativos");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "modalidades_ensino");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "etapas_ensino");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "escolas");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "disciplinas_etapas_ensino");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "disciplinas");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "anos_letivos");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "alunos_turmas");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "alunos");

            migrationBuilder.CreateIndex(
                name: "IX_users_cpf",
                table: "users",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
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
                name: "IX_professores_matricula",
                table: "professores",
                column: "matricula",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_etapas_ensino_sigla",
                table: "etapas_ensino",
                column: "sigla",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_escolas_codigo_inep",
                table: "escolas",
                column: "codigo_inep",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_disciplinas_codigo",
                table: "disciplinas",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_anos_letivos_ano_referencia",
                table: "anos_letivos",
                column: "ano_referencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alunos_cpf_aluno",
                table: "alunos",
                column: "cpf_aluno",
                unique: true,
                filter: "cpf_aluno IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_alunos_matricula",
                table: "alunos",
                column: "matricula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alunos_nome_nascimento_responsavel",
                table: "alunos",
                columns: new[] { "nome", "data_nascimento", "responsavel_nome1" },
                unique: true,
                filter: "cpf_aluno IS NULL");
        }
    }
}
