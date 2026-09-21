using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAluno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alunos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    matricula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    data_nascimento = table.Column<DateTime>(type: "date", nullable: false),
                    sexo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cor_raca = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    necessidade_especial = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    responsavel_nome1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    responsavel_cpf1 = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    responsavel_telefone1 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    responsavel_nome2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cpf_aluno = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    certidao_nascimento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    endereco_completo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EscolaId = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "ATIVO_AGUARDANDO_ENTURMACAO"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alunos", x => x.id);
                    table.CheckConstraint("CK_alunos_sexo", "sexo IN ('MASCULINO', 'FEMININO')");
                    table.ForeignKey(
                        name: "FK_alunos_escolas",
                        column: x => x.EscolaId,
                        principalTable: "escolas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alunos_cpf_aluno",
                table: "alunos",
                column: "cpf_aluno",
                unique: true,
                filter: "cpf_aluno IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_alunos_escola_id",
                table: "alunos",
                column: "EscolaId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alunos");
        }
    }
}
