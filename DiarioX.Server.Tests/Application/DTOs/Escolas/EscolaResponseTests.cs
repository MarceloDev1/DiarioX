using DiarioX.Server.Application.DTOs.Escolas;

namespace DiarioX.Server.Tests.Application.DTOs.Escolas;

public class EscolaResponseTests
{
    [Fact]
    public void Constructor_AssignsAllProperties()
    {
        var response = new EscolaResponse(
            Id: 42,
            CodigoInep: "12345678",
            Nome: "Escola Modelo",
            Cnpj: "12.345.678/0001-90",
            Telefone: "(11) 99999-0000",
            EmailInstitucional: "contato@escola.edu.br",
            Municipio: "São Paulo",
            EnderecoCompleto: "Rua das Flores, 123 - Centro",
            Status: "Ativa",
            CpfDiretor: "123.456.789-09"
        );

        Assert.Equal(42, response.Id);
        Assert.Equal("12345678", response.CodigoInep);
        Assert.Equal("Escola Modelo", response.Nome);
        Assert.Equal("12.345.678/0001-90", response.Cnpj);
        Assert.Equal("(11) 99999-0000", response.Telefone);
        Assert.Equal("contato@escola.edu.br", response.EmailInstitucional);
        Assert.Equal("São Paulo", response.Municipio);
        Assert.Equal("Rua das Flores, 123 - Centro", response.EnderecoCompleto);
        Assert.Equal("Ativa", response.Status);
        Assert.Equal("123.456.789-09", response.CpfDiretor);
    }

    [Fact]
    public void Constructor_AllowsNullCpfDiretor()
    {
        var response = new EscolaResponse(
            Id: 1,
            CodigoInep: "00000000",
            Nome: "Escola Sem Diretor",
            Cnpj: "00.000.000/0001-00",
            Telefone: "(11) 0000-0000",
            EmailInstitucional: "sem@escola.edu.br",
            Municipio: "Rio de Janeiro",
            EnderecoCompleto: "Av. Atlântica, 1 - Copacabana",
            Status: "Inativa",
            CpfDiretor: null
        );

        Assert.Null(response.CpfDiretor);
        Assert.Equal("Inativa", response.Status);
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        var a = new EscolaResponse(
            1, "11111111", "Escola A", "11.111.111/0001-11", "(11) 1111-1111",
            "a@x.com", "Cidade A", "Rua A", "Ativa", "111.111.111-11");
        var b = new EscolaResponse(
            1, "11111111", "Escola A", "11.111.111/0001-11", "(11) 1111-1111",
            "a@x.com", "Cidade A", "Rua A", "Ativa", "111.111.111-11");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void With_ChangingOneProperty_PreservesOthers()
    {
        var original = new EscolaResponse(
            10, "22222222", "Escola Original", "22.222.222/0001-22", "(11) 2222-2222",
            "orig@x.com", "Cidade B", "Rua B", "Ativa", "222.222.222-22");

        var changed = original with { Status = "Inativa" };

        Assert.Equal("Inativa", changed.Status);
        Assert.Equal(original.Id, changed.Id);
        Assert.Equal(original.CodigoInep, changed.CodigoInep);
        Assert.Equal(original.Nome, changed.Nome);
        Assert.Equal(original.Telefone, changed.Telefone);
        Assert.Equal(original.EmailInstitucional, changed.EmailInstitucional);
        Assert.Equal(original.Municipio, changed.Municipio);
        Assert.Equal(original.EnderecoCompleto, changed.EnderecoCompleto);
        Assert.Equal(original.CpfDiretor, changed.CpfDiretor);
        Assert.NotEqual(original, changed);
    }
}
