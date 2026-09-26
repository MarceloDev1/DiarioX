using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Application.Auth;

public record AcaoPermissao(string Id, string Nome);

public record ModuloPermissao(string Id, string Nome, IReadOnlyList<string> Acoes);

/// <summary>
/// Catálogo das permissões do sistema. Cada permissão é "modulo.acao"; os perfis de cada
/// instituição recebem um subconjunto delas (PerfilPermissao). O Administrador global ignora
/// o catálogo e pode tudo.
/// </summary>
public static class Permissoes
{
    public const string Visualizar = "visualizar";
    public const string Criar = "criar";
    public const string Editar = "editar";
    public const string Excluir = "excluir";

    public static readonly IReadOnlyList<AcaoPermissao> Acoes =
    [
        new(Visualizar, "Visualizar"),
        new(Criar, "Criar"),
        new(Editar, "Editar"),
        new(Excluir, "Excluir"),
    ];

    private static readonly string[] Crud = [Visualizar, Criar, Editar, Excluir];

    public static readonly IReadOnlyList<ModuloPermissao> Modulos =
    [
        new("escolas", "Escolas", Crud),
        new("modalidades-ensino", "Modalidades de Ensino", Crud),
        new("etapas-ensino", "Etapas de Ensino", Crud),
        new("anos-letivos", "Anos Letivos", Crud),
        new("disciplinas", "Disciplinas", Crud),
        new("turmas", "Turmas", Crud),
        new("professores", "Professores", Crud),
        new("alocacao-professor", "Alocação de Professor", [Visualizar, Criar, Excluir]),
        // Enturmar e remanejar alteram a situação do aluno: exigem "alunos.editar".
        new("alunos", "Alunos", Crud),
        new("chamada", "Chamada", Crud),
        new("usuarios", "Usuários", Crud),
        new("configuracoes", "Configurações (permissões)", [Visualizar, Editar]),
    ];

    public static readonly IReadOnlySet<string> Todas = Modulos
        .SelectMany(m => m.Acoes.Select(a => Codigo(m.Id, a)))
        .ToHashSet(StringComparer.Ordinal);

    public static string Codigo(string modulo, string acao) => $"{modulo}.{acao}";

    // Permissões usadas nos atributos dos controllers.
    public static class Escolas
    {
        public const string Criar = "escolas.criar";
        public const string Editar = "escolas.editar";
        public const string Excluir = "escolas.excluir";
    }

    public static class ModalidadesEnsino
    {
        public const string Criar = "modalidades-ensino.criar";
        public const string Editar = "modalidades-ensino.editar";
        public const string Excluir = "modalidades-ensino.excluir";
    }

    public static class EtapasEnsino
    {
        public const string Criar = "etapas-ensino.criar";
        public const string Editar = "etapas-ensino.editar";
        public const string Excluir = "etapas-ensino.excluir";
    }

    public static class AnosLetivos
    {
        public const string Criar = "anos-letivos.criar";
        public const string Editar = "anos-letivos.editar";
        public const string Excluir = "anos-letivos.excluir";
    }

    public static class Disciplinas
    {
        public const string Criar = "disciplinas.criar";
        public const string Editar = "disciplinas.editar";
        public const string Excluir = "disciplinas.excluir";
    }

    public static class Turmas
    {
        public const string Criar = "turmas.criar";
        public const string Editar = "turmas.editar";
        public const string Excluir = "turmas.excluir";
    }

    public static class Professores
    {
        public const string Visualizar = "professores.visualizar";
        public const string Criar = "professores.criar";
        public const string Editar = "professores.editar";
        public const string Excluir = "professores.excluir";
    }

    public static class AlocacaoProfessor
    {
        public const string Visualizar = "alocacao-professor.visualizar";
        public const string Criar = "alocacao-professor.criar";
        public const string Excluir = "alocacao-professor.excluir";
    }

    public static class Alunos
    {
        public const string Visualizar = "alunos.visualizar";
        public const string Criar = "alunos.criar";
        public const string Editar = "alunos.editar";
        public const string Excluir = "alunos.excluir";
    }

    public static class Chamada
    {
        public const string Visualizar = "chamada.visualizar";
        public const string Criar = "chamada.criar";
        public const string Editar = "chamada.editar";
        public const string Excluir = "chamada.excluir";
    }

    public static class Usuarios
    {
        public const string Visualizar = "usuarios.visualizar";
        public const string Criar = "usuarios.criar";
        public const string Editar = "usuarios.editar";
        public const string Excluir = "usuarios.excluir";
    }

    public static class Configuracoes
    {
        public const string Visualizar = "configuracoes.visualizar";
        public const string Editar = "configuracoes.editar";
    }

    /// <summary>
    /// Matriz inicial de cada instituição, por nome de perfil. Perfis ausentes começam sem permissões.
    /// </summary>
    public static IReadOnlyCollection<string> PadraoDoPerfil(string perfilNome)
    {
        if (Is(perfilNome, Perfil.Gerencia))
            return Todas.ToList();

        if (Is(perfilNome, Perfil.Diretor))
            return Todas.Where(p => !p.StartsWith("configuracoes.", StringComparison.Ordinal)).ToList();

        if (Is(perfilNome, Perfil.Secretario))
        {
            return Modulos
                .Where(m => m.Id is not ("usuarios" or "configuracoes"))
                .Select(m => Codigo(m.Id, Visualizar))
                .Concat([
                    Alunos.Criar, Alunos.Editar,
                    Turmas.Criar, Turmas.Editar,
                    Professores.Criar, Professores.Editar,
                    AlocacaoProfessor.Criar, AlocacaoProfessor.Excluir,
                    Chamada.Criar, Chamada.Editar,
                ])
                .ToList();
        }

        if (Is(perfilNome, Perfil.Financeiro))
            return [Alunos.Visualizar];

        if (Is(perfilNome, Perfil.Professor))
            return ["turmas.visualizar", "disciplinas.visualizar", Alunos.Visualizar,
                Chamada.Visualizar, Chamada.Criar, Chamada.Editar];

        return [];
    }

    private static bool Is(string nome, string perfil) => string.Equals(nome, perfil, StringComparison.OrdinalIgnoreCase);
}
