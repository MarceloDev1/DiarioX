using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DiarioX.Server.Tests.API;

/// <summary>
/// Listagem e exclusão de professores alocados pela API real.
/// </summary>
public class ProfessorAlocacoesApiTests : IClassFixture<TenancyApiFactory>
{
    private const string HostEscolaB = "http://escola-b.dev.localhost";

    private readonly TenancyApiFactory _factory;
    private static readonly Dictionary<TenancyApiFactory, (int UsuarioId, int AlocacaoId, int AlocacaoExclusaoId)> Cenarios = new();

    private readonly int _usuarioId;
    private readonly int _alocacaoId;
    private readonly int _alocacaoExclusaoId;

    public ProfessorAlocacoesApiTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();

        lock (Cenarios)
        {
            if (!Cenarios.TryGetValue(factory, out var cenario))
                Cenarios[factory] = cenario = Semear(factory);

            (_usuarioId, _alocacaoId, _alocacaoExclusaoId) = cenario;
        }
    }

    private static (int, int, int) Semear(TenancyApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenantId = factory.TenantB.Id;
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        var escola = new Escola { TenantId = tenantId, Nome = "Escola Sul", CodigoInep = "20" };
        var etapa = new EtapaEnsino { TenantId = tenantId, ModalidadeEnsinoId = factory.ModalidadeBId, Nome = "5º Ano", Sigla = "5A" };
        var ano = new AnoLetivo
        {
            TenantId = tenantId, AnoReferencia = hoje.Year, TipoPeriodo = AnoLetivo.TipoBimestral,
            DataInicio = hoje.AddDays(-60), DataTermino = hoje.AddDays(200),
        };
        var disciplina = new Disciplina { TenantId = tenantId, Nome = "Português", Codigo = "POR" };
        var usuario = new User { TenantId = tenantId, Email = "aloc@rede.com", Cpf = "86288366757" };
        var perfil = new Perfil { Nome = "Alocador" };
        db.AddRange(escola, etapa, ano, disciplina, usuario, perfil);
        db.SaveChanges();

        var turma = new Turma
        {
            TenantId = tenantId, AnoLetivoId = ano.Id, EscolaId = escola.Id, ModalidadeEnsinoId = factory.ModalidadeBId,
            EtapaEnsinoId = etapa.Id, NomeIdentificador = "A", NomeCompleto = "5º Ano A", VagasOfertadas = 30,
        };
        var professor = new Professor
        {
            TenantId = tenantId, Nome = "Professor Alocado", Cpf = "11122233396", Email = "pa@rede.com",
            Telefone = "11987654321", DataNascimento = new DateTime(1985, 5, 15),
        };
        db.AddRange(turma, professor);
        db.SaveChanges();

        var outraDisciplina = new Disciplina { TenantId = tenantId, Nome = "Matemática", Codigo = "MAT" };
        db.Add(outraDisciplina);
        db.SaveChanges();

        var alocacao = new ProfessorAlocacao { TenantId = tenantId, ProfessorId = professor.Id, TurmaId = turma.Id, DisciplinaId = disciplina.Id };
        var alocacaoExclusao = new ProfessorAlocacao { TenantId = tenantId, ProfessorId = professor.Id, TurmaId = turma.Id, DisciplinaId = outraDisciplina.Id };
        db.AddRange(
            alocacao,
            alocacaoExclusao,
            new UsuarioPerfil { UsuarioId = usuario.Id, PerfilId = perfil.Id });
        db.AddRange(
            new[] { Permissoes.AlocacaoProfessor.Visualizar, Permissoes.AlocacaoProfessor.Excluir }
                .Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfil.Id, Permissao = p }));
        db.SaveChanges();

        return (usuario.Id, alocacao.Id, alocacaoExclusao.Id);
    }

    [Fact]
    public async Task Listar_RetornaAlocacoesComEscolaModalidadeEEtapa()
    {
        var lista = await Client().GetFromJsonAsync<JsonElement>("/api/professor-alocacoes");

        var item = lista.EnumerateArray().Single(x => x.GetProperty("id").GetInt32() == _alocacaoId);
        Assert.Equal("Professor Alocado", item.GetProperty("professorNome").GetString());
        Assert.Equal("Escola Sul", item.GetProperty("escolaNome").GetString());
        Assert.Equal("5º Ano", item.GetProperty("etapaNome").GetString());
        Assert.Equal("5º Ano A", item.GetProperty("turmaNome").GetString());
        Assert.Equal("Português", item.GetProperty("disciplinaNome").GetString());
        Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("modalidadeNome").GetString()));
    }

    [Fact]
    public async Task Excluir_RemoveAAlocacaoDaLista()
    {
        var client = Client();

        var resposta = await client.DeleteAsync($"/api/professor-alocacoes/{_alocacaoExclusaoId}");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var lista = await client.GetFromJsonAsync<JsonElement>("/api/professor-alocacoes");
        Assert.DoesNotContain(lista.EnumerateArray(), x => x.GetProperty("id").GetInt32() == _alocacaoExclusaoId);
        Assert.Contains(lista.EnumerateArray(), x => x.GetProperty("id").GetInt32() == _alocacaoId);
    }

    private HttpClient Client()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(HostEscolaB),
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken());
        return client;
    }

    private string CreateToken()
    {
        var jwt = _factory.Services.GetRequiredService<IConfiguration>().GetSection("Jwt");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, _usuarioId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AppClaimTypes.TenantId, _factory.TenantB.Id.ToString()),
            new(AppClaimTypes.TenantSlug, _factory.TenantB.Slug),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
