namespace DiarioX.Server.Application.DTOs.Escolas;

public record EscolaResponse(
    int Id,
    string CodigoInep,
    string Nome,
    string Cnpj,
    string Telefone,
    string EmailInstitucional,
    string Municipio,
    string EnderecoCompleto,
    string Status,
    string? CpfDiretor
);
