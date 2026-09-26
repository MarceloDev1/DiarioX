using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissaoRelatorios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Instituições que já têm matriz de permissões recebem o módulo novo com o mesmo padrão
            // de Permissoes.PadraoDoPerfil. As que ainda não têm recebem a matriz completa no startup.
            migrationBuilder.Sql("""
                INSERT INTO perfis_permissoes (tenant_id, perfil_id, permissao)
                SELECT t.id, p.id, 'relatorios.visualizar'
                FROM tenants t
                JOIN perfis p ON LOWER(p.nome) IN ('gerência', 'diretor', 'secretário')
                WHERE EXISTS (SELECT 1 FROM perfis_permissoes pp WHERE pp.tenant_id = t.id)
                ON CONFLICT (tenant_id, perfil_id, permissao) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM perfis_permissoes WHERE permissao LIKE 'relatorios.%';");
        }
    }
}
