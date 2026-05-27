using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfilAndUsuarioPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "perfis",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfis", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios_perfis",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    perfil_id = table.Column<int>(type: "integer", nullable: false),
                    escola_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios_perfis", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_perfis_users_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_perfis_perfis_perfil_id",
                        column: x => x.perfil_id,
                        principalTable: "perfis",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_perfis_escolas_escola_id",
                        column: x => x.escola_id,
                        principalTable: "escolas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_perfis_global",
                table: "usuarios_perfis",
                columns: new[] { "usuario_id", "perfil_id" },
                unique: true,
                filter: "escola_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_perfis_escola",
                table: "usuarios_perfis",
                columns: new[] { "usuario_id", "perfil_id", "escola_id" },
                unique: true,
                filter: "escola_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_perfis_perfil_id",
                table: "usuarios_perfis",
                column: "perfil_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_perfis_escola_id",
                table: "usuarios_perfis",
                column: "escola_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "usuarios_perfis");
            migrationBuilder.DropTable(name: "perfis");
        }
    }
}
