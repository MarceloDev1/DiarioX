using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEscola : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "escolas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_inep = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email_institucional = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    municipio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    endereco_completo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    diretor_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ATIVO")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_escolas", x => x.id);
                    table.ForeignKey(
                        name: "FK_escolas_users_diretor_id",
                        column: x => x.diretor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.CheckConstraint("CK_escolas_status", "status IN ('ATIVO', 'INATIVO')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_escolas_codigo_inep",
                table: "escolas",
                column: "codigo_inep",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_escolas_diretor_id",
                table: "escolas",
                column: "diretor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "escolas");
        }
    }
}