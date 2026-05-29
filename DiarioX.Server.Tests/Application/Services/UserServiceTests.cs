using DiarioX.Server.Application.DTOs.Users;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
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

        userRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new[]
            {
                BuildUser(1, "user1@x.com", "52998224725", perfilId: 2, perfilNome: "Coordenador"),
                BuildUser(2, "user2@x.com", "11144477735", perfilId: null, perfilNome: null)
            });

        var service = new UserService(userRepository.Object, perfilRepository.Object, usuarioPerfilRepository.Object);

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

        userRepository.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((User?)null);

        var service = new UserService(userRepository.Object, perfilRepository.Object, usuarioPerfilRepository.Object);

        var result = await service.GetByIdAsync(42);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WhenPerfilNotFound_ReturnsValidationError()
    {
        var userRepository = new Mock<IUserRepository>();
        var perfilRepository = new Mock<IPerfilRepository>();
        var usuarioPerfilRepository = new Mock<IUsuarioPerfilRepository>();

        var request = BuildValidRequest(perfilId: 99);
        perfilRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Perfil?)null);

        var service = new UserService(userRepository.Object, perfilRepository.Object, usuarioPerfilRepository.Object);

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
            r => r.AddAsync(It.Is<UsuarioPerfil>(up => up.UsuarioId == 15 && up.PerfilId == 3 && up.EscolaId == null)),
            Times.Once);
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

        var existingUsuarioPerfil = new UsuarioPerfil
        {
            Id = 70,
            UsuarioId = 5,
            PerfilId = 1,
            EscolaId = null,
            Usuario = existingUser,
            Perfil = new Perfil { Id = 1, Nome = "Antigo" }
        };

        service.UsuarioPerfilRepository.Setup(r => r.GetGlobalByUsuarioIdAsync(5)).ReturnsAsync(existingUsuarioPerfil);
        service.UserRepository
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(BuildUser(5, "usuario@x.com", "52998224725", perfilId: 5, perfilNome: "Novo Perfil"));

        var result = await service.Service.UpdateAsync(5, request);

        Assert.True(result.Success);
        Assert.Equal("Usuario atualizado com sucesso.", result.Message);
        Assert.NotNull(result.User);
        Assert.Equal(5, result.User!.PerfilId);

        service.UserRepository.Verify(r => r.UpdateAsync(It.Is<User>(u => u.Id == 5)), Times.Once);
        service.UsuarioPerfilRepository.Verify(r => r.UpdateAsync(It.Is<UsuarioPerfil>(up => up.Id == 70 && up.PerfilId == 5)), Times.Once);
        service.UsuarioPerfilRepository.Verify(r => r.AddAsync(It.IsAny<UsuarioPerfil>()), Times.Never);
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
        Assert.Equal("Nao e possivel remover este usuario pois ele esta associado a outros registros.", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenValid_ReturnsSuccess()
    {
        var service = BuildService();

        service.UserRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(BuildUser(10, "usuario@x.com", "52998224725", null, null));

        var result = await service.Service.DeleteAsync(10);

        Assert.True(result.Success);
        Assert.Equal("Usuario removido com sucesso.", result.Message);
        service.UserRepository.Verify(r => r.DeleteAsync(10), Times.Once);
    }

    private static (UserService Service, Mock<IUserRepository> UserRepository, Mock<IPerfilRepository> PerfilRepository, Mock<IUsuarioPerfilRepository> UsuarioPerfilRepository) BuildService()
    {
        var userRepository = new Mock<IUserRepository>();
        var perfilRepository = new Mock<IPerfilRepository>();
        var usuarioPerfilRepository = new Mock<IUsuarioPerfilRepository>();

        userRepository.Setup(r => r.GetByEmailOrCpfAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepository.Setup(r => r.GetByCpfAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var service = new UserService(userRepository.Object, perfilRepository.Object, usuarioPerfilRepository.Object);
        return (service, userRepository, perfilRepository, usuarioPerfilRepository);
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
