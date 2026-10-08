using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrazoNotasERegraRecuperacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "substituicao_recuperacao",
                table: "regras_avaliacao",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "SUBSTITUI_MEDIA");

            migrationBuilder.AddColumn<DateOnly>(
                name: "prazo_lancamento_notas",
                table: "periodos_avaliativos",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_regras_avaliacao_substituicao",
                table: "regras_avaliacao",
                sql: "substituicao_recuperacao IN ('SUBSTITUI_MEDIA', 'MEDIA_COM_RECUPERACAO', 'LIMITADA_A_MEDIA_APROVACAO')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_regras_avaliacao_substituicao",
                table: "regras_avaliacao");

            migrationBuilder.DropColumn(
                name: "substituicao_recuperacao",
                table: "regras_avaliacao");

            migrationBuilder.DropColumn(
                name: "prazo_lancamento_notas",
                table: "periodos_avaliativos");
        }
    }
}
