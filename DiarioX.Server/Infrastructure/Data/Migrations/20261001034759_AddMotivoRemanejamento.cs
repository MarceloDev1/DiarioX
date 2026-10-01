using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMotivoRemanejamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_alunos_turmas_motivo_desenturmacao",
                table: "alunos_turmas");

            migrationBuilder.AddCheckConstraint(
                name: "CK_alunos_turmas_motivo_desenturmacao",
                table: "alunos_turmas",
                sql: "motivo_desenturmacao IS NULL OR motivo_desenturmacao IN ('REESTRUTURACAO_INTERNA', 'NAO_COMPARECEU', 'FALECIMENTO', 'ERRO_MATRICULA_ENTURMACAO', 'OUTROS', 'TRANSFERENCIA', 'REMANEJAMENTO')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_alunos_turmas_motivo_desenturmacao",
                table: "alunos_turmas");

            // Vínculos encerrados por remanejamento não cabem na lista antiga de motivos.
            migrationBuilder.Sql("UPDATE alunos_turmas SET motivo_desenturmacao = NULL, observacao_desenturmacao = NULL WHERE motivo_desenturmacao = 'REMANEJAMENTO';");

            migrationBuilder.AddCheckConstraint(
                name: "CK_alunos_turmas_motivo_desenturmacao",
                table: "alunos_turmas",
                sql: "motivo_desenturmacao IS NULL OR motivo_desenturmacao IN ('REESTRUTURACAO_INTERNA', 'NAO_COMPARECEU', 'FALECIMENTO', 'ERRO_MATRICULA_ENTURMACAO', 'OUTROS', 'TRANSFERENCIA')");
        }
    }
}
