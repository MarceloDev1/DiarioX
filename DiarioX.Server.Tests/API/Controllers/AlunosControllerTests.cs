using DiarioX.Server.API.Controllers;
using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DiarioX.Server.Tests.API.Controllers;

public class AlunosControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOkWithAlunos()
    {
        var service = new Mock<IAlunoService>();
        var alunos = new List<AlunoResponse> { BuildResponse(1), BuildResponse(2) };
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(alunos);

        var controller = BuildController(service.Object);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<AlunoResponse>>(ok.Value);
        Assert.Equal(2, payload.Count());
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var service = new Mock<IAlunoService>();
        var aluno = BuildResponse(9);
        service.Setup(s => s.GetByIdAsync(9)).ReturnsAsync(new AlunoCommandResult(true, string.Empty, aluno));

        var controller = BuildController(service.Object);

        var result = await controller.GetById(9);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(aluno, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFoundWithMessage()
    {
        var service = new Mock<IAlunoService>();
        service.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(new AlunoCommandResult(false, "Aluno não encontrado.", Error: AlunoResultError.NotFound));

        var controller = BuildController(service.Object);

        var result = await controller.GetById(7);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Aluno não encontrado.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreatedAtAction()
    {
        var service = new Mock<IAlunoService>();
        var request = BuildRequest();
        var aluno = BuildResponse(15);
        var commandResult = new AlunoCommandResult(true, "Aluno cadastrado com sucesso!", aluno);

        service.Setup(s => s.CreateAsync(request)).ReturnsAsync(commandResult);

        var controller = BuildController(service.Object);

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(AlunosController.GetById), created.ActionName);
        Assert.Equal(15, created.RouteValues?["id"]);
        Assert.Equal(aluno, created.Value);
    }

    [Fact]
    public async Task Create_WhenValidationError_ReturnsBadRequest()
    {
        var service = new Mock<IAlunoService>();
        var request = BuildRequest();
        var commandResult = new AlunoCommandResult(false, "Nome completo é obrigatório.", Error: AlunoResultError.Validation);

        service.Setup(s => s.CreateAsync(request)).ReturnsAsync(commandResult);

        var controller = BuildController(service.Object);

        var result = await controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Nome completo é obrigatório.", GetMessage(badRequest.Value));
    }

    [Fact]
    public async Task Create_WhenConflict_ReturnsConflict()
    {
        var service = new Mock<IAlunoService>();
        var request = BuildRequest();
        var commandResult = new AlunoCommandResult(false, "Atenção: Já existe um aluno cadastrado no sistema com estes dados.", Error: AlunoResultError.Conflict);

        service.Setup(s => s.CreateAsync(request)).ReturnsAsync(commandResult);

        var controller = BuildController(service.Object);

        var result = await controller.Create(request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Atenção: Já existe um aluno cadastrado no sistema com estes dados.", GetMessage(conflict.Value));
    }

    [Fact]
    public async Task Create_WhenEscolaNaoEncontrada_ReturnsBadRequest()
    {
        var service = new Mock<IAlunoService>();
        var request = BuildRequest();
        var commandResult = new AlunoCommandResult(false, "Escola não encontrada.", Error: AlunoResultError.DependencyNotFound);

        service.Setup(s => s.CreateAsync(request)).ReturnsAsync(commandResult);

        var controller = BuildController(service.Object);

        var result = await controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Escola não encontrada.", GetMessage(badRequest.Value));
    }

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        var service = new Mock<IAlunoService>();
        var request = BuildRequest();
        var aluno = BuildResponse(5);
        var commandResult = new AlunoCommandResult(true, "Aluno atualizado com sucesso!", aluno);

        service.Setup(s => s.UpdateAsync(5, request)).ReturnsAsync(commandResult);

        var controller = BuildController(service.Object);

        var result = await controller.Update(5, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(aluno, ok.Value);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new Mock<IAlunoService>();
        var request = BuildRequest();
        var commandResult = new AlunoCommandResult(false, "Aluno não encontrado.", Error: AlunoResultError.NotFound);

        service.Setup(s => s.UpdateAsync(100, request)).ReturnsAsync(commandResult);

        var controller = BuildController(service.Object);

        var result = await controller.Update(100, request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Aluno não encontrado.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsOkWithMessage()
    {
        var service = new Mock<IAlunoService>();
        var commandResult = new AlunoCommandResult(true, "Aluno removido com sucesso!");

        service.Setup(s => s.DeleteAsync(3)).ReturnsAsync(commandResult);

        var controller = BuildController(service.Object);

        var result = await controller.Delete(3);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Aluno removido com sucesso!", GetMessage(ok.Value));
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new Mock<IAlunoService>();
        var commandResult = new AlunoCommandResult(false, "Aluno não encontrado.", Error: AlunoResultError.NotFound);

        service.Setup(s => s.DeleteAsync(4)).ReturnsAsync(commandResult);

        var controller = BuildController(service.Object);

        var result = await controller.Delete(4);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Aluno não encontrado.", GetMessage(notFound.Value));
    }

    private static AlunoRequest BuildRequest() => new()
    {
        Nome = "Maria da Silva",
        DataNascimento = new DateTime(2015, 4, 10),
        Sexo = "FEMININO",
        CorRaca = "PARDA",
        NecessidadeEspecial = false,
        ResponsavelNome1 = "Joana da Silva",
        ResponsavelCpf1 = "52998224725",
        ResponsavelTelefone1 = "11999998888",
        CertidaoNascimento = "123456 01 55 2015 1 00001 001 0000001-11",
        Cep = "01310200",
        EnderecoCompleto = "Av. Paulista",
        Numero = "1000",
        Bairro = "Bela Vista",
        EscolaId = 1,
    };

    private static AlunosController BuildController(IAlunoService alunoService)
        => new(alunoService, Mock.Of<IRemanejamentoAlunoService>());

    private static AlunoResponse BuildResponse(int id) => new(
        id,
        "20260001",
        "Maria da Silva",
        new DateTime(2015, 4, 10),
        "FEMININO",
        "PARDA",
        false,
        null,
        "123456 01 55 2015 1 00001 001 0000001-11",
        "Joana da Silva",
        "52998224725",
        "11999998888",
        null,
        "01310200",
        "Av. Paulista",
        "1000",
        "Bela Vista",
        1,
        "Escola Modelo",
        "ATIVO_AGUARDANDO_ENTURMACAO",
        DateTime.UtcNow,
        null
    );

    private static string? GetMessage(object? value)
    {
        if (value is null)
            return null;

        var property = value.GetType().GetProperty("message");
        return property?.GetValue(value)?.ToString();
    }
}
