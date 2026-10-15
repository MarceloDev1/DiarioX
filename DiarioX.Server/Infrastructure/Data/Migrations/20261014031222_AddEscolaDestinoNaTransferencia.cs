using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEscolaDestinoNaTransferencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "escola_destino_id",
                table: "transferencias",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_escola_destino_id",
                table: "transferencias",
                column: "escola_destino_id");

            migrationBuilder.AddForeignKey(
                name: "FK_transferencias_escolas_escola_destino_id",
                table: "transferencias",
                column: "escola_destino_id",
                principalTable: "escolas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transferencias_escolas_escola_destino_id",
                table: "transferencias");

            migrationBuilder.DropIndex(
                name: "IX_transferencias_escola_destino_id",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "escola_destino_id",
                table: "transferencias");
        }
    }
}
