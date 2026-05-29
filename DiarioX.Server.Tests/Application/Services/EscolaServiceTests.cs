using DiarioX.Server.Application.DTOs.Escolas;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class EscolaServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsMappedResponses()
    {
        var fixture = BuildService();
        fixture.EscolaRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new[]
            {
                BuildEscola(1, "11111111000191"),
                BuildEscola(2, "22222222000191")
            });

        var result = (await fixture.Service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("11111111000191", result[0].Cnpj);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var fixture = BuildService();
        fixture.EscolaRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Escola?)null);

        var result = await fixture.Service.GetByIdAsync(10);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WhenCodigoInepMissing_ReturnsValidationError()
    {
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.CodigoInep = "";

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(EscolaResultError.Validation, result.Error);
        Assert.Equal("Codigo INEP obrigatorio.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailInvalid_ReturnsValidationError()
    {
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.EmailInstitucional = "email-invalido";

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(EscolaResultError.Validation, result.Error);
        Assert.Equal("E-mail institucional invalido.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenStatusInvalid_ReturnsValidationError()
    {
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.Status = "BLOQUEADO";

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(EscolaResultError.Validation, result.Error);
        Assert.Equal("Status invalido. Valores permitidos: ATIVO ou INATIVO.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenCodigoInepAlreadyExists_ReturnsConflict()
    {
        var fixture = BuildService();
        var request = BuildValidRequest();

        fixture.EscolaRepository
            .Setup(r => r.ExistsByCodigoInepAsync("12345678", null))
            .ReturnsAsync(true);

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(EscolaResultError.Conflict, result.Error);
        Assert.Equal("Ja existe uma escola com este codigo INEP.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenCpfDiretorNotFound_ReturnsUserNotFound()
    {
        var fixture = BuildService();
        var request = BuildValidRequest();

        fixture.EscolaRepository
            .Setup(r => r.ExistsByCodigoInepAsync("12345678", null))
            .ReturnsAsync(false);

        fixture.UserRepository
            .Setup(r => r.GetByCpfAsync("52998224725"))
            .ReturnsAsync((User?)null);

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(EscolaResultError.UserNotFound, result.Error);
        Assert.Equal("Nenhum usuario encontrado com o CPF informado.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenValidWithDiretor_CreatesEscolaAndUpdatesPerfilEscola()
    {
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.Cnpj = "11.111.111/0001-91";
        request.EmailInstitucional = "  CONTATO@ESCOLA.COM ";
        request.Status = "ativo";

        fixture.EscolaRepository
            .Setup(r => r.ExistsByCodigoInepAsync("12345678", null))
            .ReturnsAsync(false);

        var diretor = new User { Id = 77, Cpf = "52998224725", Email = "diretor@x.com" };
        fixture.UserRepository.Setup(r => r.GetByCpfAsync("52998224725")).ReturnsAsync(diretor);

        Escola? captured = null;
        fixture.EscolaRepository
            .Setup(r => r.AddAsync(It.IsAny<Escola>()))
            .Callback<Escola>(e => captured = e)
            .ReturnsAsync((Escola e) =>
            {
                e.Id = 25;
                return e;
            });

        fixture.EscolaRepository
            .Setup(r => r.GetByIdAsync(25))
            .ReturnsAsync(BuildEscola(25, "11111111000191", diretorCpf: "52998224725", diretorId: 77));

        var perfilGlobal = new UsuarioPerfil
        {
            Id = 10,
            UsuarioId = 77,
            PerfilId = 2,
            EscolaId = null,
            Usuario = diretor,
            Perfil = new Perfil { Id = 2, Nome = "Diretor" }
        };

        fixture.UsuarioPerfilRepository
            .Setup(r => r.GetGlobalByUsuarioIdAsync(77))
            .ReturnsAsync(perfilGlobal);

        var result = await fixture.Service.CreateAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Escola);
        Assert.Equal("Escola cadastrada com sucesso.", result.Message);
        Assert.Equal(25, result.Escola!.Id);

        Assert.NotNull(captured);
        Assert.Equal("11111111000191", captured!.Cnpj);
        Assert.Equal("contato@escola.com", captured.EmailInstitucional);
        Assert.Equal("ATIVO", captured.Status);
        Assert.Equal(77, captured.DiretorId);

        fixture.UsuarioPerfilRepository.Verify(
            r => r.UpdateAsync(It.Is<UsuarioPerfil>(up => up.Id == 10 && up.EscolaId == 25)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenEscolaMissing_ReturnsNotFound()
    {
        var fixture = BuildService();
        fixture.EscolaRepository.Setup(r => r.GetByIdAsync(90)).ReturnsAsync((Escola?)null);

        var result = await fixture.Service.UpdateAsync(90, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(EscolaResultError.NotFound, result.Error);
        Assert.Equal("Escola nao encontrada.", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenValidWithoutDiretor_UpdatesEscola()
    {
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.CpfDiretor = null;
        request.Status = "inativo";

        var existing = BuildEscola(4, "12345678000199", diretorCpf: "52998224725", diretorId: 33);
        fixture.EscolaRepository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(existing);
        fixture.EscolaRepository.Setup(r => r.ExistsByCodigoInepAsync("12345678", 4)).ReturnsAsync(false);
        fixture.EscolaRepository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(existing);

        var result = await fixture.Service.UpdateAsync(4, request);

        Assert.True(result.Success);
        Assert.Equal("Escola atualizada com sucesso.", result.Message);
        Assert.Equal(Escola.StatusInativo, existing.Status);
        Assert.Null(existing.DiretorId);
        fixture.EscolaRepository.Verify(r => r.UpdateAsync(existing), Times.Once);
        fixture.UsuarioPerfilRepository.Verify(r => r.UpdateAsync(It.IsAny<UsuarioPerfil>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFound()
    {
        var fixture = BuildService();
        fixture.EscolaRepository.Setup(r => r.GetByIdAsync(101)).ReturnsAsync((Escola?)null);

        var result = await fixture.Service.DeleteAsync(101);

        Assert.False(result.Success);
        Assert.Equal(EscolaResultError.NotFound, result.Error);
        Assert.Equal("Escola nao encontrada.", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsSuccess()
    {
        var fixture = BuildService();
        fixture.EscolaRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildEscola(5, "12345678000199"));

        var result = await fixture.Service.DeleteAsync(5);

        Assert.True(result.Success);
        Assert.Equal("Escola removida com sucesso.", result.Message);
        fixture.EscolaRepository.Verify(r => r.DeleteAsync(5), Times.Once);
    }

    private static (EscolaService Service, Mock<IEscolaRepository> EscolaRepository, Mock<IUserRepository> UserRepository, Mock<IUsuarioPerfilRepository> UsuarioPerfilRepository) BuildService()
    {
        var escolaRepository = new Mock<IEscolaRepository>();
        var userRepository = new Mock<IUserRepository>();
        var usuarioPerfilRepository = new Mock<IUsuarioPerfilRepository>();

        var service = new EscolaService(escolaRepository.Object, userRepository.Object, usuarioPerfilRepository.Object);
        return (service, escolaRepository, userRepository, usuarioPerfilRepository);
    }

    private static EscolaRequest BuildValidRequest() => new()
    {
        CodigoInep = "12345678",
        Nome = "Escola Modelo",
        Cnpj = "11111111000191",
        Telefone = "(11) 99999-0000",
        EmailInstitucional = "contato@escola.com",
        Municipio = "Sao Paulo",
        EnderecoCompleto = "Rua Central, 100",
        Status = "ATIVO",
        CpfDiretor = "52998224725"
    };

    private static Escola BuildEscola(int id, string cnpj, string? diretorCpf = null, int? diretorId = null)
    {
        return new Escola
        {
            Id = id,
            CodigoInep = "12345678",
            Nome = "Escola Modelo",
            Cnpj = cnpj,
            Telefone = "(11) 99999-0000",
            EmailInstitucional = "contato@escola.com",
            Municipio = "Sao Paulo",
            EnderecoCompleto = "Rua Central, 100",
            Status = Escola.StatusAtivo,
            DiretorId = diretorId,
            Diretor = diretorCpf is null ? null : new User { Cpf = diretorCpf }
        };
    }
}
