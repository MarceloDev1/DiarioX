namespace DiarioX.Server.Domain.Entities;

public class Perfil
{
    public const string Administrador = "Administrador";
    public const string Gerencia = "Gerência";
    public const string Diretor = "Diretor";
    public const string Secretario = "Secretário";
    public const string Financeiro = "Financeiro";
    public const string Professor = "Professor";
    public const string Estudante = "Estudante";
    public const string Responsavel = "Responsável";

    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}
