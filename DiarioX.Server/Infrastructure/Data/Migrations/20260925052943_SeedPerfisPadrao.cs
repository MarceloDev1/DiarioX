using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiarioX.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedPerfisPadrao : Migration
    {
        private static readonly (string Nome, string Descricao)[] PerfisPadrao =
        {
            ("Administrador", "Usuário de TI"),
            ("Gerência", "Dono da Escola, sócio ou responsável legal"),
            ("Diretor", "Funcionário da escola responsável por gerenciar atividades escolares"),
            ("Secretário", "Funcionário de uma escola responsável por auxiliar nas atividades escolares"),
            ("Financeiro", "Funcionário de uma escola responsável por gerenciar as atividades financeiras"),
            ("Professor", "Pessoa que vai atualizar os dados dos diários de classe"),
            ("Estudante", "Pessoa que estuda na escola nos ensinos fundamental e EJA"),
            ("Responsável", "Pai ou responsável pelo(a) estudante"),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insere apenas os perfis que ainda não existem (por nome), preservando perfis já cadastrados.
            foreach (var (nome, descricao) in PerfisPadrao)
            {
                migrationBuilder.Sql($"""
                    INSERT INTO perfis (nome, descricao)
                    SELECT '{nome}', '{descricao}'
                    WHERE NOT EXISTS (SELECT 1 FROM perfis WHERE LOWER(nome) = LOWER('{nome}'));
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove apenas os perfis padrão que não estão vinculados a nenhum usuário.
            foreach (var (nome, _) in PerfisPadrao)
            {
                migrationBuilder.Sql($"""
                    DELETE FROM perfis p
                    WHERE p.nome = '{nome}'
                      AND NOT EXISTS (SELECT 1 FROM usuarios_perfis up WHERE up.perfil_id = p.id);
                    """);
            }
        }
    }
}
