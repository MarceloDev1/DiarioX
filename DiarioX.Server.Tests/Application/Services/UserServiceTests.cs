using DiarioX.Server.Application.DTOs.Users;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class UserServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsMappedUsers()
    {
        var userRepository = new Mock<IUserRepository>();
        var perfilRepository = new Mock<IPerfilRepository>();
        var usuarioPerfilRepository = new Mock<IUsuarioPerfilRepository>();
        var emailNotificationService = new Mock<IEmailNotificationService>();
        var logger = new Mock<ILogger<UserService>>();

        userRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new[]
            {
                BuildUser(1, "user1@x.com", "52998224725", perfilId: 2, perfilNome: "Coordenador"),
                BuildUser(2, "user2@x.com", "11144477735", perfilId: null, perfilNome: null)
            });

        var service = new UserService(userRepository.Object, perfilRepository.Object, usuarioPerfilRepository.Object, emailNotificationService.Object, logger.Object, Mock.Of<IEscolaRepository>(), Mock.Of<ITenantContext>());

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[0].PerfilId);
        Assert.Equal("Coordenador", result[0].PerfilNome);
        Assert.Equal(2, result[1].Id);
        Assert.Null(result[1].PerfilId);
        Assert.Null(result[1].PerfilNome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var userRepository = new Mock<IUserRepository>();
        var perfilRepository = new Mock<IPerfilRepository>();
        var usuarioPerfilRepository = new Mock<IUsuarioPerfilRepository>();
        var emailNotificationService = new Mock<IEmailNotificationService>();
        var logger = new Mock<ILogger<UserService>>();

        userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((User?)null);

        var service = new UserService(userRepository.Object, perfilRepository.Object, usuarioPerfilRepository.Object, emailNotificationService.Object, logger.Object, Mock.Of<IEscolaRepository>(), Mock.Of<ITenantContext>());

        var result = await service.GetByIdAsync(42);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WhenPerfilNotFound_ReturnsValidationError()
    {
        var userRepository = new Mock<IUserRepository>();
        var perfilRepository = new Mock<IPerfilRepository>();
        var usuarioPerfilRepository = new Mock<IUsuarioPerfilRepository>();
        var emailNotificationService = new Mock<IEmailNotificationService>();
        var logger = new Mock<ILogger<UserService>>();

        var request = BuildValidRequest(perfilId: 99);
        perfilRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Perfil?)null);

        var service = new UserService(userRepository.Object, perfilRepository.Object, usuarioPerfilRepository.Object, emailNotificationService.Object, logger.Object, Mock.Of<IEscolaRepository>(), Mock.Of<ITenantContext>());

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(UserResultError.Validation, result.Error);
        Assert.Equal("Perfil nao encontrado.", result.Message);
        userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailInvalid_ReturnsValidationError()
    {
        var service = BuildService();

        var request = BuildValidRequest();
        request.Email = "email-invalido";

        var result = await service.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(UserResultError.Validation, result.Error);
        Assert.Equal("E-mail invalido.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenCpfInvalid_ReturnsValidationError()
    {
        var service = BuildService();

        var request = BuildValidRequest();
        request.Cpf = "12345678901";

        var result = await service.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(UserResultError.Validation, result.Error);
        Assert.Equal("CPF invalido (informe 11 digitos validos).", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenPasswordPolicyInvalid_ReturnsValidationError()
    {
        var service = BuildService();

        var request = BuildValidRequest();
        request.Senha = "fraca";

        var result = await service.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(UserResultError.Validation, result.Error);
        Assert.StartsWith("Senha fora da politica de seguranca", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailExists_ReturnsConflict()
    {
        var service = BuildService();
        var request = BuildValidRequest();

        service.UserRepository
            .Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com"))
            .ReturnsAsync(BuildUser(8, "usuario@x.com", "52998224725", null, null));

        var result = await service.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(UserResultError.Conflict, result.Error);
        Assert.Equal("Ja existe um usuario com este e-mail.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenCpfExists_ReturnsConflict()
    {
        var service = BuildService();
        var request = BuildValidRequest();

        service.UserRepository
            .Setup(r => r.GetByCpfAsync("52998224725"))
            .ReturnsAsync(BuildUser(9, "outro@x.com", "52998224725", null, null));

        var result = await service.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(UserResultError.Conflict, result.Error);
        Assert.Equal("Ja existe um usuario com este CPF.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_PersistsAndReturnsMappedUser()
    {
        var service = BuildService();
        var request = BuildValidRequest(perfilId: 3);
        request.Email = "  Usuario@X.com  ";
        request.Cpf = "529.982.247-25";
        request.Status = "ativo";
        request.Senha = "Senha@123";

        User? capturedAddedUser = null;

        service.UserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedAddedUser = u)
            .ReturnsAsync((User u) =>
            {
                u.Id = 15;
                return u;
            });

        service.PerfilRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Perfil { Id = 3, Nome = "Professor" });

        service.UserRepository
            .Setup(r => r.GetByIdAsync(15))
            .ReturnsAsync(BuildUser(15, "usuario@x.com", "52998224725", perfilId: 3, perfilNome: "Professor"));

        var result = await service.Service.CreateAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal("Usuario cadastrado com sucesso.", result.Message);
        Assert.Equal("usuario@x.com", result.User!.Email);
        Assert.Equal("52998224725", result.User.Cpf);
        Assert.Equal(3, result.User.PerfilId);

        Assert.NotNull(capturedAddedUser);
        Assert.Equal("usuario@x.com", capturedAddedUser!.Email);
        Assert.Equal("52998224725", capturedAddedUser.Cpf);
        Assert.Equal("ATIVO", capturedAddedUser.Status);
        Assert.NotEqual("Senha@123", capturedAddedUser.SenhaHash);
        Assert.True(capturedAddedUser.VerifyPassword("Senha@123"));

        service.UsuarioPerfilRepository.Verify(
            r => r.SubstituirAsync(15, 3, It.Is<IReadOnlyCollection<int>>(e => e.Count == 0)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEscolas_SavesOnePerfilPerEscola()
    {
        var service = BuildService();
        var request = BuildValidRequest(perfilId: 4);
        request.EscolaIds = [8, 3, 8];
        service.PerfilRepository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(new Perfil { Id = 4, Nome = Perfil.Diretor });
        service.EscolaRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Escola { Id = id });
        service.UserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => { u.Id = 15; return u; });
        service.UserRepository.Setup(r => r.GetByIdAsync(15)).ReturnsAsync(BuildUser(15, "usuario@x.com", "52998224725", 4, Perfil.Diretor));

        var result = await service.Service.CreateAsync(request);

        Assert.True(result.Success);
        service.UsuarioPerfilRepository.Verify(
            r => r.SubstituirAsync(15, 4, It.Is<IReadOnlyCollection<int>>(e => e.SequenceEqual(new[] { 3, 8 }))),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEscolaForaDoAlcance_ReturnsValidationError()
    {
        var service = BuildService();
        var request = BuildValidRequest(perfilId: 4);
        request.EscolaIds = [9];
        service.PerfilRepository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(new Perfil { Id = 4, Nome = Perfil.Diretor });
        service.EscolaRepository.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((Escola?)null);

        var result = await service.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal("Escola de atuação não encontrada.", result.Message);
        service.UserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEscolasSemPerfil_ReturnsValidationError()
    {
        var service = BuildService();
        var request = BuildValidRequest();
        request.EscolaIds = [3];

        var result = await service.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(UserResultError.Validation, result.Error);
        service.UserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenRequesterIsScoped_CannotGrantWholeNetwork()
    {
        var service = BuildService(escopo: [3]);
        var request = BuildValidRequest(perfilId: 4);
        service.PerfilRepository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(new Perfil { Id = 4, Nome = Perfil.Secretario });

        var result = await service.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Contains("Selecione as escolas de atuação", result.Message);
        service.UserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenRequesterIsScoped_AllowsProfessorWithoutEscolas()
    {
        // O usuário criado junto com o professor segue as escolas em que ele leciona.
        var service = BuildService(escopo: [3]);
        var request = BuildValidRequest(perfilId: 6);
        service.PerfilRepository.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(new Perfil { Id = 6, Nome = Perfil.Professor });
        service.UserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => { u.Id = 15; return u; });
        service.UserRepository.Setup(r => r.GetByIdAsync(15)).ReturnsAsync(BuildUser(15, "usuario@x.com", "52998224725", 6, Perfil.Professor));

        var result = await service.Service.CreateAsync(request);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateAsync_WhenRequesterIsScoped_CannotChangeUserFromWholeNetwork()
    {
        var service = BuildService(escopo: [3]);
        var request = BuildValidRequest(perfilId: 4);
        request.EscolaIds = [3];
        service.PerfilRepository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(new Perfil { Id = 4, Nome = Perfil.Diretor });
        service.EscolaRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Escola { Id = 3 });
        service.UserRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildUser(5, "usuario@x.com", "52998224725", 2, Perfil.Gerencia));

        var result = await service.Service.UpdateAsync(5, request);

        Assert.False(result.Success);
        Assert.Contains("usuários que atuam apenas nas suas escolas", result.Message);
        service.UserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenAccessIsUnchanged_DoesNotRewritePerfis()
    {
        // Um usuário restrito pode editar os dados de alguém da rede toda sem mexer no acesso dele.
        var service = BuildService(escopo: [3]);
        var request = BuildValidRequest(perfilId: 2);
        service.PerfilRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Perfil { Id = 2, Nome = Perfil.Gerencia });
        service.UserRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildUser(5, "usuario@x.com", "52998224725", 2, Perfil.Gerencia));

        var result = await service.Service.UpdateAsync(5, request);

        Assert.True(result.Success);
        service.UsuarioPerfilRepository.Verify(r => r.SubstituirAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_MapsEscolasDeAtuacao()
    {
        var service = BuildService();
        var user = BuildUser(5, "usuario@x.com", "52998224725", null, null);
        var perfil = new Perfil { Id = 4, Nome = Perfil.Diretor };
        user.UsuariosPerfis.Add(new UsuarioPerfil { UsuarioId = 5, PerfilId = 4, Perfil = perfil, EscolaId = 9 });
        user.UsuariosPerfis.Add(new UsuarioPerfil { UsuarioId = 5, PerfilId = 4, Perfil = perfil, EscolaId = 3 });
        service.UserRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

        var result = await service.Service.GetByIdAsync(5);

        Assert.Equal(4, result!.PerfilId);
        Assert.Equal([3, 9], result.EscolaIds);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserMissing_ReturnsNotFound()
    {
        var service = BuildService();

        service.UserRepository.Setup(r => r.GetByIdAsync(88)).ReturnsAsync((User?)null);

        var result = await service.Service.UpdateAsync(88, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(UserResultError.NotFound, result.Error);
        Assert.Equal("Usuario nao encontrado.", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailAlreadyUsedByAnotherUser_ReturnsConflict()
    {
        var service = BuildService();

        service.UserRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildUser(5, "atual@x.com", "52998224725", null, null));
        service.UserRepository
            .Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com"))
            .ReturnsAsync(BuildUser(99, "usuario@x.com", "11144477735", null, null));

        var result = await service.Service.UpdateAsync(5, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(UserResultError.Conflict, result.Error);
        Assert.Equal("Ja existe um usuario com este e-mail.", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenValidWithDifferentPerfil_UpdatesUsuarioPerfil()
    {
        var service = BuildService();
        var request = BuildValidRequest(perfilId: 5);

        var existingUser = BuildUser(5, "atual@x.com", "52998224725", perfilId: 1, perfilNome: "Antigo");
        service.UserRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existingUser);
        service.PerfilRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Perfil { Id = 5, Nome = "Novo Perfil" });

        service.UserRepository
            .SetupSequence(r => r.GetByIdAsync(5))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(BuildUser(5, "usuario@x.com", "52998224725", perfilId: 5, perfilNome: "Novo Perfil"));

        var result = await service.Service.UpdateAsync(5, request);

        Assert.True(result.Success);
        Assert.Equal("Usuario atualizado com sucesso.", result.Message);
        Assert.NotNull(result.User);
        Assert.Equal(5, result.User!.PerfilId);

        service.UserRepository.Verify(r => r.UpdateAsync(It.Is<User>(u => u.Id == 5)), Times.Once);
        service.UsuarioPerfilRepository.Verify(r => r.SubstituirAsync(5, 5, It.Is<IReadOnlyCollection<int>>(e => e.Count == 0)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenPerfilIsAdministrador_ReturnsValidationError()
    {
        var service = BuildService();
        var request = BuildValidRequest(perfilId: 1);
        service.PerfilRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Perfil { Id = 1, Nome = Perfil.Administrador });

        var result = await service.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(UserResultError.Validation, result.Error);
        Assert.Contains("Administrador", result.Message);
        service.UserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenPerfilIsAdministrador_ReturnsValidationError()
    {
        var service = BuildService();
        var request = BuildValidRequest(perfilId: 1);

        service.UserRepository
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(BuildUser(5, "atual@x.com", "52998224725", perfilId: 2, perfilNome: "Professor"));
        service.PerfilRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Perfil { Id = 1, Nome = "administrador" });

        var result = await service.Service.UpdateAsync(5, request);

        Assert.False(result.Success);
        Assert.Equal(UserResultError.Validation, result.Error);
        service.UserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        service.UsuarioPerfilRepository.Verify(r => r.SubstituirAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryThrows_ReturnsConflict()
    {
        var service = BuildService();

        service.UserRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(BuildUser(10, "usuario@x.com", "52998224725", null, null));
        service.UserRepository.Setup(r => r.DeleteAsync(10)).ThrowsAsync(new InvalidOperationException("fk"));

        var result = await service.Service.DeleteAsync(10);

        Assert.False(result.Success);
        Assert.Equal(UserResultError.Conflict, result.Error);
        Assert.Equal("Não é possivel remover este usuário pois ele esta associado a outros registros.", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenValid_ReturnsSuccess()
    {
        var service = BuildService();

        service.UserRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(BuildUser(10, "usuario@x.com", "52998224725", null, null));

        var result = await service.Service.DeleteAsync(10);

        Assert.True(result.Success);
        Assert.Equal("Usuário removido com sucesso.", result.Message);
        service.UserRepository.Verify(r => r.DeleteAsync(10), Times.Once);
    }

    private static (UserService Service, Mock<IUserRepository> UserRepository, Mock<IPerfilRepository> PerfilRepository, Mock<IUsuarioPerfilRepository> UsuarioPerfilRepository, Mock<IEscolaRepository> EscolaRepository) BuildService(IReadOnlyList<int>? escopo = null)
    {
        var escolaRepository = new Mock<IEscolaRepository>();
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(t => t.EscolaIds).Returns(escopo);
        var userRepository = new Mock<IUserRepository>();
        var perfilRepository = new Mock<IPerfilRepository>();
        var usuarioPerfilRepository = new Mock<IUsuarioPerfilRepository>();
        var emailNotificationService = new Mock<IEmailNotificationService>();
        var logger = new Mock<ILogger<UserService>>();

        userRepository.Setup(r => r.GetByEmailOrCpfAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepository.Setup(r => r.GetByCpfAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var service = new UserService(userRepository.Object, perfilRepository.Object, usuarioPerfilRepository.Object, emailNotificationService.Object, logger.Object, escolaRepository.Object, tenantContext.Object);
        return (service, userRepository, perfilRepository, usuarioPerfilRepository, escolaRepository);
    }

    private static UserRequest BuildValidRequest(int? perfilId = null) => new()
    {
        Email = "usuario@x.com",
        Cpf = "52998224725",
        DataNascimento = new DateTime(1999, 12, 31),
        Senha = "Senha@123",
        Status = User.StatusAtivo,
        PerfilId = perfilId
    };

    private static User BuildUser(int id, string email, string cpf, int? perfilId, string? perfilNome)
    {
        var user = new User
        {
            Id = id,
            Email = email,
            Cpf = cpf,
            Status = User.StatusAtivo,
            DataNascimento = new DateTime(2000, 1, 1),
            CreatedAt = new DateTime(2025, 1, 1)
        };

        if (perfilId.HasValue)
        {
            user.UsuariosPerfis.Add(new UsuarioPerfil
            {
                Id = 1,
                UsuarioId = id,
                Usuario = user,
                PerfilId = perfilId.Value,
                Perfil = new Perfil { Id = perfilId.Value, Nome = perfilNome ?? string.Empty },
                EscolaId = null
            });
        }

        return user;
    }
}
