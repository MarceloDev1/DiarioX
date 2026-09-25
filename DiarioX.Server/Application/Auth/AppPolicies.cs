namespace DiarioX.Server.Application.Auth;

/// <summary>
/// Políticas de autorização nomeadas. A política padrão (usuário autenticado com uma
/// instituição selecionada) é configurada no Program.cs e vale para toda rota sem atributo.
/// </summary>
public static class AppPolicies
{
    public const string GlobalAdmin = "GlobalAdmin";
}
