namespace DiarioX.Server.Application.DTOs.Conteudos;

public class HabilidadeBnccRequest
{
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int DisciplinaId { get; set; }
    public List<int> EtapasEnsinoIds { get; set; } = new();
    public bool Ativa { get; set; } = true;
}

public record HabilidadeBnccEtapaResponse(int Id, string Nome);

public record HabilidadeBnccResponse(
    int Id,
    string Codigo,
    string Descricao,
    int DisciplinaId,
    string DisciplinaNome,
    bool Ativa,
    IReadOnlyList<HabilidadeBnccEtapaResponse> EtapasEnsino
);

public enum HabilidadeBnccResultError
{
    None, Validation, Conflict, NotFound
}

public record HabilidadeBnccCommandResult(
    bool Success,
    string Message,
    HabilidadeBnccResponse? Habilidade = null,
    HabilidadeBnccResultError Error = HabilidadeBnccResultError.None
);
