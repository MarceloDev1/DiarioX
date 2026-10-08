using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFrequenciaDiariaEEncerramentoPeriodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "encerrado",
                table: "periodos_avaliativos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "encerrado_em",
                table: "periodos_avaliativos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo_frequencia",
                table: "etapas_ensino",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "POR_AULA");

            migrationBuilder.AlterColumn<int>(
                name: "disciplina_id",
                table: "chamadas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddCheckConstraint(
                name: "CK_etapas_ensino_tipo_frequencia",
                table: "etapas_ensino",
                sql: "tipo_frequencia IN ('POR_AULA', 'DIARIA')");

            migrationBuilder.CreateIndex(
                name: "IX_chamadas_turma_data_diaria",
                table: "chamadas",
                columns: new[] { "tenant_id", "turma_id", "data" },
                unique: true,
                filter: "disciplina_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_etapas_ensino_tipo_frequencia",
                table: "etapas_ensino");

            migrationBuilder.DropIndex(
                name: "IX_chamadas_turma_data_diaria",
                table: "chamadas");

            migrationBuilder.DropColumn(
                name: "encerrado",
                table: "periodos_avaliativos");

            migrationBuilder.DropColumn(
                name: "encerrado_em",
                table: "periodos_avaliativos");

            migrationBuilder.DropColumn(
                name: "tipo_frequencia",
                table: "etapas_ensino");

            migrationBuilder.AlterColumn<int>(
                name: "disciplina_id",
                table: "chamadas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
