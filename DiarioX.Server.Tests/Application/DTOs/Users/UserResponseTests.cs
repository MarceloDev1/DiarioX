using DiarioX.Server.Application.DTOs.Users;

namespace DiarioX.Server.Tests.Application.DTOs.Users;

public class UserResponseTests
{
    private static UserResponse Sample(
        int id = 1,
        string email = "user@x.com",
        string cpf = "52998224725",
        DateTime? dataNascimento = null,
        string status = "ATIVO",
        DateTime? ultimoAcesso = null,
        DateTime? createdAt = null,
        int? perfilId = 10,
        string? perfilNome = "Diretor")
        => new(
            id,
            email,
            cpf,
            dataNascimento,
            status,
            ultimoAcesso,
            createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            perfilId,
            perfilNome
        );

    [Fact]
    public void Constructor_AssignsAllProperties()
    {
        var dataNascimento = new DateTime(1990, 5, 20, 0, 0, 0, DateTimeKind.Utc);
        var ultimoAcesso = new DateTime(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);
        var createdAt = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var response = new UserResponse(
            Id: 42,
            Email: "fulano@x.com",
            Cpf: "52998224725",
            DataNascimento: dataNascimento,
            Status: "ATIVO",
            UltimoAcesso: ultimoAcesso,
            CreatedAt: createdAt,
            PerfilId: 7,
            PerfilNome: "Professor"
        );

        Assert.Equal(42, response.Id);
        Assert.Equal("fulano@x.com", response.Email);
        Assert.Equal("52998224725", response.Cpf);
        Assert.Equal(dataNascimento, response.DataNascimento);
        Assert.Equal("ATIVO", response.Status);
        Assert.Equal(ultimoAcesso, response.UltimoAcesso);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal(7, response.PerfilId);
        Assert.Equal("Professor", response.PerfilNome);
    }

    [Fact]
    public void Constructor_AllowsNullableFieldsToBeNull()
    {
        var response = Sample(
            dataNascimento: null,
            ultimoAcesso: null,
            perfilId: null,
            perfilNome: null);

        Assert.Null(response.DataNascimento);
        Assert.Null(response.UltimoAcesso);
        Assert.Null(response.PerfilId);
        Assert.Null(response.PerfilNome);
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        var a = Sample();
        var b = Sample();

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a == b);
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("Email")]
    [InlineData("Cpf")]
    [InlineData("Status")]
    [InlineData("PerfilId")]
    [InlineData("PerfilNome")]
    public void Records_DifferingByOneProperty_AreNotEqual(string property)
    {
        var baseResponse = Sample();
        var changed = property switch
        {
            "Id" => baseResponse with { Id = 999 },
            "Email" => baseResponse with { Email = "outro@x.com" },
            "Cpf" => baseResponse with { Cpf = "11144477735" },
            "Status" => baseResponse with { Status = "INATIVO" },
            "PerfilId" => baseResponse with { PerfilId = 20 },
            "PerfilNome" => baseResponse with { PerfilNome = "Coordenador" },
            _ => baseResponse
        };

        Assert.NotEqual(baseResponse, changed);
    }

    [Fact]
    public void With_PreservesUnchangedProperties()
    {
        var original = Sample(
            id: 1,
            email: "a@x.com",
            cpf: "52998224725",
            status: "ATIVO",
            perfilId: 10,
            perfilNome: "Diretor");

        var changed = original with { Status = "INATIVO" };

        Assert.Equal("INATIVO", changed.Status);
        Assert.Equal(original.Id, changed.Id);
        Assert.Equal(original.Email, changed.Email);
        Assert.Equal(original.Cpf, changed.Cpf);
        Assert.Equal(original.DataNascimento, changed.DataNascimento);
        Assert.Equal(original.UltimoAcesso, changed.UltimoAcesso);
        Assert.Equal(original.CreatedAt, changed.CreatedAt);
        Assert.Equal(original.PerfilId, changed.PerfilId);
        Assert.Equal(original.PerfilNome, changed.PerfilNome);
    }

    [Fact]
    public void ToString_ContainsKeyProperties()
    {
        var response = Sample(id: 123, email: "x@y.com", status: "ATIVO");

        var text = response.ToString();

        Assert.Contains("123", text);
        Assert.Contains("x@y.com", text);
        Assert.Contains("ATIVO", text);
    }
}
