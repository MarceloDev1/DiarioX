using DiarioX.Server.API.Controllers;
using DiarioX.Server.Application.DTOs.Escolas;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DiarioX.Server.Tests.API.Controllers;

public class EscolasControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOkWithEscolas()
    {
        var service = new Mock<IEscolaService>();
        var escolas = new List<EscolaResponse>
        {
            BuildResponse(1),
            BuildResponse(2)
        };
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(escolas);

        var controller = new EscolasController(service.Object);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<EscolaResponse>>(ok.Value);
        Assert.Equal(2, payload.Count());
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var service = new Mock<IEscolaService>();
        var escola = BuildResponse(9);

        service.Setup(s => s.GetByIdAsync(9)).ReturnsAsync(escola);

        var controller = new EscolasController(service.Object);

        var result = await controller.GetById(9);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(escola, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFoundWithMessage()
    {
        var service = new Mock<IEscolaService>();

        service.Setup(s => s.GetByIdAsync(7)).ReturnsAsync((EscolaResponse?)null);

        var controller = new EscolasController(service.Object);

        var result = await controller.GetById(7);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Escola nao encontrada.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreatedAtAction()
    {
        var service = new Mock<IEscolaService>();
        var request = BuildRequest();
        var escola = BuildResponse(15);
        var commandResult = new EscolaCommandResult(true, "Escola cadastrada com sucesso.", escola);

        service.Setup(s => s.CreateAsync(request)).ReturnsAsync(commandResult);

        var controller = new EscolasController(service.Object);

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(EscolasController.GetById), created.ActionName);
        Assert.Equal(15, created.RouteValues?["id"]);
        Assert.Equal(escola, created.Value);
    }

    [Fact]
    public async Task Create_WhenConflict_ReturnsConflict()
    {
        var service = new Mock<IEscolaService>();
        var request = BuildRequest();
        var commandResult = new EscolaCommandResult(false, "Ja existe uma escola com este codigo INEP.", Error: EscolaResultError.Conflict);

        service.Setup(s => s.CreateAsync(request)).ReturnsAsync(commandResult);

        var controller = new EscolasController(service.Object);

        var result = await controller.Create(request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Ja existe uma escola com este codigo INEP.", GetMessage(conflict.Value));
    }

    [Fact]
    public async Task Create_WhenUserNotFound_ReturnsUnprocessableEntity()
    {
        var service = new Mock<IEscolaService>();
        var request = BuildRequest();
        var commandResult = new EscolaCommandResult(false, "Nenhum usuario encontrado com o CPF informado.", Error: EscolaResultError.UserNotFound);

        service.Setup(s => s.CreateAsync(request)).ReturnsAsync(commandResult);

        var controller = new EscolasController(service.Object);

        var result = await controller.Create(request);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.Equal("Nenhum usuario encontrado com o CPF informado.", GetMessage(unprocessable.Value));
    }

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        var service = new Mock<IEscolaService>();
        var request = BuildRequest();
        var escola = BuildResponse(5);
        var commandResult = new EscolaCommandResult(true, "Escola atualizada com sucesso.", escola);

        service.Setup(s => s.UpdateAsync(5, request)).ReturnsAsync(commandResult);

        var controller = new EscolasController(service.Object);

        var result = await controller.Update(5, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(escola, ok.Value);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new Mock<IEscolaService>();
        var request = BuildRequest();
        var commandResult = new EscolaCommandResult(false, "Escola nao encontrada.", Error: EscolaResultError.NotFound);

        service.Setup(s => s.UpdateAsync(100, request)).ReturnsAsync(commandResult);

        var controller = new EscolasController(service.Object);

        var result = await controller.Update(100, request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Escola nao encontrada.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsOkWithMessage()
    {
        var service = new Mock<IEscolaService>();
        var commandResult = new EscolaCommandResult(true, "Escola removida com sucesso.");

        service.Setup(s => s.DeleteAsync(3)).ReturnsAsync(commandResult);

        var controller = new EscolasController(service.Object);

        var result = await controller.Delete(3);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Escola removida com sucesso.", GetMessage(ok.Value));
    }

    [Fact]
    public async Task Delete_WhenValidationError_ReturnsBadRequest()
    {
        var service = new Mock<IEscolaService>();
        var commandResult = new EscolaCommandResult(false, "Erro de validacao.", Error: EscolaResultError.Validation);

        service.Setup(s => s.DeleteAsync(4)).ReturnsAsync(commandResult);

        var controller = new EscolasController(service.Object);

        var result = await controller.Delete(4);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Erro de validacao.", GetMessage(badRequest.Value));
    }

    private static EscolaRequest BuildRequest() => new()
    {
        CodigoInep = "12345678",
        Nome = "Escola Teste",
        Cnpj = "12345678000199",
        Telefone = "(11) 99999-0000",
        EmailInstitucional = "contato@escola.com",
        Municipio = "Sao Paulo",
        EnderecoCompleto = "Rua A, 123",
        Status = "ATIVO",
        CpfDiretor = "52998224725"
    };

    private static EscolaResponse BuildResponse(int id) => new(
        id,
        "12345678",
        $"Escola {id}",
        "12345678000199",
        "(11) 99999-0000",
        "contato@escola.com",
        "Sao Paulo",
        "Rua A, 123",
        "ATIVO",
        "52998224725"
    );

    private static string? GetMessage(object? value)
    {
        if (value is null)
            return null;

        var property = value.GetType().GetProperty("message");
        return property?.GetValue(value)?.ToString();
    }
}
