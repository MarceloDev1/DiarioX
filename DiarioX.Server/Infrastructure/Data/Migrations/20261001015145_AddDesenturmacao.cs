using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDesenturmacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_alunos_turmas_periodo",
                table: "alunos_turmas");

            migrationBuilder.AddColumn<string>(
                name: "motivo_desenturmacao",
                table: "alunos_turmas",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "observacao_desenturmacao",
                table: "alunos_turmas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_alunos_turmas_motivo_desenturmacao",
                table: "alunos_turmas",
                sql: "motivo_desenturmacao IS NULL OR motivo_desenturmacao IN ('REESTRUTURACAO_INTERNA', 'NAO_COMPARECEU', 'FALECIMENTO', 'ERRO_MATRICULA_ENTURMACAO', 'OUTROS')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_alunos_turmas_periodo",
                table: "alunos_turmas",
                sql: "data_fim IS NULL OR data_fim >= data_inicio - 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_alunos_turmas_motivo_desenturmacao",
                table: "alunos_turmas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_alunos_turmas_periodo",
                table: "alunos_turmas");

            migrationBuilder.DropColumn(
                name: "motivo_desenturmacao",
                table: "alunos_turmas");

            migrationBuilder.DropColumn(
                name: "observacao_desenturmacao",
                table: "alunos_turmas");

            // Vínculos desfeitos no mesmo dia (data_fim = data_inicio - 1) não cabem na regra antiga.
            migrationBuilder.Sql("UPDATE alunos_turmas SET data_fim = data_inicio WHERE data_fim < data_inicio;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_alunos_turmas_periodo",
                table: "alunos_turmas",
                sql: "data_fim IS NULL OR data_fim >= data_inicio");
        }
    }
}
