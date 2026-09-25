using DiarioX.Server.API.Controllers;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DiarioX.Server.Tests.API.Controllers;

public class PerfisControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOkWithPerfisExceptAdministrador()
    {
        var repo = new Mock<IPerfilRepository>();
        var perfis = new List<Perfil>
        {
            new() { Id = 1, Nome = "Administrador", Descricao = "Acesso total" },
            new() { Id = 2, Nome = "Professor", Descricao = "Acesso pedagógico" }
        };

        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(perfis);

        var controller = new PerfisController(repo.Object);

        var result = await controller.GetAll();

        // Administrador é exclusivo dos usuários globais e não é oferecido dentro da instituição.
        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<Perfil>>(ok.Value).ToList();
        var perfil = Assert.Single(payload);
        Assert.Equal("Professor", perfil.Nome);
    }
}
