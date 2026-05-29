using DiarioX.Server.API.Controllers;
using DiarioX.Server.Application.DTOs.Users;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DiarioX.Server.Tests.API.Controllers;

public class UsersControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOkWithUsers()
    {
        var serviceMock = new Mock<IUserService>();
        var users = new List<UserResponse>
        {
            CreateUserResponse(id: 1),
            CreateUserResponse(id: 2)
        };

        serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

        var controller = new UsersController(serviceMock.Object);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<UserResponse>>(ok.Value);
        Assert.Equal(2, payload.Count());
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkWithUser()
    {
        var serviceMock = new Mock<IUserService>();
        var user = CreateUserResponse(id: 10);

        serviceMock.Setup(s => s.GetByIdAsync(10)).ReturnsAsync(user);

        var controller = new UsersController(serviceMock.Object);

        var result = await controller.GetById(10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal(10, payload.Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFoundWithMessage()
    {
        var serviceMock = new Mock<IUserService>();

        serviceMock.Setup(s => s.GetByIdAsync(7)).ReturnsAsync((UserResponse?)null);

        var controller = new UsersController(serviceMock.Object);

        var result = await controller.GetById(7);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Usuario nao encontrado.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreatedAtAction()
    {
        var serviceMock = new Mock<IUserService>();
        var request = CreateRequest();
        var createdUser = CreateUserResponse(id: 33);
        var commandResult = new UserCommandResult(
            Success: true,
            Message: "Usuario criado com sucesso.",
            User: createdUser);

        serviceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(commandResult);

        var controller = new UsersController(serviceMock.Object);

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(UsersController.GetById), created.ActionName);
        Assert.Equal(33, created.RouteValues?["id"]);
        Assert.Equal(createdUser, created.Value);
    }

    [Fact]
    public async Task Create_WhenConflict_ReturnsConflictWithMessage()
    {
        var serviceMock = new Mock<IUserService>();
        var request = CreateRequest();
        var commandResult = new UserCommandResult(
            Success: false,
            Message: "Email ja cadastrado.",
            Error: UserResultError.Conflict);

        serviceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(commandResult);

        var controller = new UsersController(serviceMock.Object);

        var result = await controller.Create(request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Email ja cadastrado.", GetMessage(conflict.Value));
    }

    [Fact]
    public async Task Create_WhenValidationError_ReturnsBadRequestWithMessage()
    {
        var serviceMock = new Mock<IUserService>();
        var request = CreateRequest();
        var commandResult = new UserCommandResult(
            Success: false,
            Message: "Dados invalidos.",
            Error: UserResultError.Validation);

        serviceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(commandResult);

        var controller = new UsersController(serviceMock.Object);

        var result = await controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Dados invalidos.", GetMessage(badRequest.Value));
    }

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOkWithUpdatedUser()
    {
        var serviceMock = new Mock<IUserService>();
        var request = CreateRequest();
        var updated = CreateUserResponse(id: 9);
        var commandResult = new UserCommandResult(
            Success: true,
            Message: "Usuario atualizado com sucesso.",
            User: updated);

        serviceMock.Setup(s => s.UpdateAsync(9, request)).ReturnsAsync(commandResult);

        var controller = new UsersController(serviceMock.Object);

        var result = await controller.Update(9, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(updated, ok.Value);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFoundWithMessage()
    {
        var serviceMock = new Mock<IUserService>();
        var request = CreateRequest();
        var commandResult = new UserCommandResult(
            Success: false,
            Message: "Usuario nao encontrado.",
            Error: UserResultError.NotFound);

        serviceMock.Setup(s => s.UpdateAsync(123, request)).ReturnsAsync(commandResult);

        var controller = new UsersController(serviceMock.Object);

        var result = await controller.Update(123, request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Usuario nao encontrado.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsOkWithMessage()
    {
        var serviceMock = new Mock<IUserService>();
        var commandResult = new UserCommandResult(
            Success: true,
            Message: "Usuario removido com sucesso.");

        serviceMock.Setup(s => s.DeleteAsync(4)).ReturnsAsync(commandResult);

        var controller = new UsersController(serviceMock.Object);

        var result = await controller.Delete(4);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Usuario removido com sucesso.", GetMessage(ok.Value));
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFoundWithMessage()
    {
        var serviceMock = new Mock<IUserService>();
        var commandResult = new UserCommandResult(
            Success: false,
            Message: "Usuario nao encontrado.",
            Error: UserResultError.NotFound);

        serviceMock.Setup(s => s.DeleteAsync(77)).ReturnsAsync(commandResult);

        var controller = new UsersController(serviceMock.Object);

        var result = await controller.Delete(77);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Usuario nao encontrado.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Delete_WhenValidationError_ReturnsBadRequestWithMessage()
    {
        var serviceMock = new Mock<IUserService>();
        var commandResult = new UserCommandResult(
            Success: false,
            Message: "Nao foi possivel remover usuario.",
            Error: UserResultError.Validation);

        serviceMock.Setup(s => s.DeleteAsync(88)).ReturnsAsync(commandResult);

        var controller = new UsersController(serviceMock.Object);

        var result = await controller.Delete(88);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Nao foi possivel remover usuario.", GetMessage(badRequest.Value));
    }

    private static UserRequest CreateRequest() => new()
    {
        Email = "teste@diariox.com",
        Cpf = "12345678901",
        DataNascimento = new DateTime(1990, 1, 1),
        Senha = "senha123",
        Status = "ATIVO",
        PerfilId = 1
    };

    private static UserResponse CreateUserResponse(int id) => new(
        Id: id,
        Email: $"usuario{id}@diariox.com",
        Cpf: "12345678901",
        DataNascimento: new DateTime(1990, 1, 1),
        Status: "ATIVO",
        UltimoAcesso: null,
        CreatedAt: DateTime.UtcNow,
        PerfilId: 1,
        PerfilNome: "Administrador"
    );

    private static string? GetMessage(object? value)
    {
        if (value is null)
            return null;

        var property = value.GetType().GetProperty("message");
        return property?.GetValue(value)?.ToString();
    }
}
