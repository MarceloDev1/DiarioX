using DiarioX.Server.API.Controllers;
using DiarioX.Server.Application.DTOs.AnosLetivos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DiarioX.Server.Tests.API.Controllers;

public class AnosLetivosControllerTests
{
    // ── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithAnos()
    {
        var service = new Mock<IAnoLetivoService>();
        service.Setup(s => s.GetAllAsync())
            .ReturnsAsync(new[] { BuildResponse(1), BuildResponse(2) });

        var result = await new AnosLetivosController(service.Object).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<AnoLetivoResponse>>(ok.Value);
        Assert.Equal(2, payload.Count());
    }

    // ── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var service = new Mock<IAnoLetivoService>();
        var response = BuildResponse(4);
        service.Setup(s => s.GetByIdAsync(4)).ReturnsAsync(response);

        var result = await new AnosLetivosController(service.Object).GetById(4);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFoundWithMessage()
    {
        var service = new Mock<IAnoLetivoService>();
        service.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((AnoLetivoResponse?)null);

        var result = await new AnosLetivosController(service.Object).GetById(99);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Ano letivo não encontrado.", GetMessage(notFound.Value));
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreatedAtAction()
    {
        var service = new Mock<IAnoLetivoService>();
        var request = BuildRequest();
        var response = BuildResponse(20);
        service.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(new AnoLetivoCommandResult(true, "Ano letivo cadastrado com sucesso.", response));

        var result = await new AnosLetivosController(service.Object).Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(AnosLetivosController.GetById), created.ActionName);
        Assert.Equal(20, created.RouteValues?["id"]);
        Assert.Equal(response, created.Value);
    }

    [Fact]
    public async Task Create_WhenValidationError_ReturnsBadRequest()
    {
        var service = new Mock<IAnoLetivoService>();
        var request = BuildRequest();
        service.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(new AnoLetivoCommandResult(false, "Ano de referência inválido.", Error: AnoLetivoResultError.Validation));

        var result = await new AnosLetivosController(service.Object).Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Ano de referência inválido.", GetMessage(badRequest.Value));
    }

    [Fact]
    public async Task Create_WhenConflict_ReturnsConflict()
    {
        var service = new Mock<IAnoLetivoService>();
        var request = BuildRequest();
        service.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(new AnoLetivoCommandResult(false, "Já existe um ano letivo cadastrado para 2026.", Error: AnoLetivoResultError.Conflict));

        var result = await new AnosLetivosController(service.Object).Create(request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Já existe um ano letivo cadastrado para 2026.", GetMessage(conflict.Value));
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOkWithResponse()
    {
        var service = new Mock<IAnoLetivoService>();
        var request = BuildRequest();
        var response = BuildResponse(7);
        service.Setup(s => s.UpdateAsync(7, request))
            .ReturnsAsync(new AnoLetivoCommandResult(true, "Ano letivo atualizado com sucesso.", response));

        var result = await new AnosLetivosController(service.Object).Update(7, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new Mock<IAnoLetivoService>();
        var request = BuildRequest();
        service.Setup(s => s.UpdateAsync(50, request))
            .ReturnsAsync(new AnoLetivoCommandResult(false, "Ano letivo não encontrado.", Error: AnoLetivoResultError.NotFound));

        var result = await new AnosLetivosController(service.Object).Update(50, request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Ano letivo não encontrado.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Update_WhenConflict_ReturnsConflict()
    {
        var service = new Mock<IAnoLetivoService>();
        var request = BuildRequest();
        service.Setup(s => s.UpdateAsync(7, request))
            .ReturnsAsync(new AnoLetivoCommandResult(false, "Já existe um ano letivo cadastrado para 2026.", Error: AnoLetivoResultError.Conflict));

        var result = await new AnosLetivosController(service.Object).Update(7, request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Já existe um ano letivo cadastrado para 2026.", GetMessage(conflict.Value));
    }

    [Fact]
    public async Task Update_WhenValidationError_ReturnsBadRequest()
    {
        var service = new Mock<IAnoLetivoService>();
        var request = BuildRequest();
        service.Setup(s => s.UpdateAsync(7, request))
            .ReturnsAsync(new AnoLetivoCommandResult(false, "A data de início deve ser anterior à data de término do ano letivo.", Error: AnoLetivoResultError.Validation));

        var result = await new AnosLetivosController(service.Object).Update(7, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("A data de início deve ser anterior à data de término do ano letivo.", GetMessage(badRequest.Value));
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsOkWithMessage()
    {
        var service = new Mock<IAnoLetivoService>();
        service.Setup(s => s.DeleteAsync(3))
            .ReturnsAsync(new AnoLetivoCommandResult(true, "Ano letivo excluído com sucesso."));

        var result = await new AnosLetivosController(service.Object).Delete(3);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Ano letivo excluído com sucesso.", GetMessage(ok.Value));
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new Mock<IAnoLetivoService>();
        service.Setup(s => s.DeleteAsync(99))
            .ReturnsAsync(new AnoLetivoCommandResult(false, "Ano letivo não encontrado.", Error: AnoLetivoResultError.NotFound));

        var result = await new AnosLetivosController(service.Object).Delete(99);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Ano letivo não encontrado.", GetMessage(notFound.Value));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AnoLetivoRequest BuildRequest() => new()
    {
        AnoReferencia = 2026,
        DataInicio = new DateOnly(2026, 2, 1),
        DataTermino = new DateOnly(2026, 12, 15),
        TipoPeriodo = AnoLetivo.TipoBimestral,
        Periodos = new List<PeriodoAvaliativoRequest>
        {
            new() { Nome = "1º Bimestre", Numero = 1, DataInicio = new DateOnly(2026, 2, 1),  DataTermino = new DateOnly(2026, 4, 30) },
            new() { Nome = "2º Bimestre", Numero = 2, DataInicio = new DateOnly(2026, 5, 1),  DataTermino = new DateOnly(2026, 7, 15) },
            new() { Nome = "3º Bimestre", Numero = 3, DataInicio = new DateOnly(2026, 7, 16), DataTermino = new DateOnly(2026, 9, 30) },
            new() { Nome = "4º Bimestre", Numero = 4, DataInicio = new DateOnly(2026, 10, 1), DataTermino = new DateOnly(2026, 12, 15) },
        }
    };

    private static AnoLetivoResponse BuildResponse(int id) => new(
        id,
        2026,
        new DateOnly(2026, 2, 1),
        new DateOnly(2026, 12, 15),
        AnoLetivo.TipoBimestral,
        new List<PeriodoAvaliativoResponse>
        {
            new(1, id, "1º Bimestre", 1, new DateOnly(2026, 2, 1),  new DateOnly(2026, 4, 30)),
            new(2, id, "2º Bimestre", 2, new DateOnly(2026, 5, 1),  new DateOnly(2026, 7, 15)),
            new(3, id, "3º Bimestre", 3, new DateOnly(2026, 7, 16), new DateOnly(2026, 9, 30)),
            new(4, id, "4º Bimestre", 4, new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 15)),
        }
    );

    private static string? GetMessage(object? value)
    {
        if (value is null) return null;
        return value.GetType().GetProperty("message")?.GetValue(value)?.ToString();
    }
}
