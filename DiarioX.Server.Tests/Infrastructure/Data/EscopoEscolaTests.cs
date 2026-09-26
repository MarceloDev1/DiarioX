using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using DiarioX.Server.Infrastructure.Repositories;
using DiarioX.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Tests.Infrastructure.Data;

/// <summary>
/// Exercita o filtro global de escola do AppDbContext e a resolução do escopo do usuário.
/// </summary>
public class EscopoEscolaTests
{
    private const int Tenant = 1;

    [Fact]
    public void SemEscopo_VeTodasAsEscolasDaInstituicao()
    {
        var seed = Seed();
        using var context = CreateContext(seed.Database, escolas: null);

        Assert.Equal(2, context.Escolas.Count());
        Assert.Equal(2, context.Turmas.Count());
        Assert.Equal(2, context.Alunos.Count());
        Assert.Equal(2, context.AlunosTurmas.Count());
        Assert.Equal(2, context.Chamadas.Count());
        Assert.Equal(2, context.ChamadasAlunos.Count());
        Assert.Equal(2, context.Professores.Count());
    }

    [Fact]
    public void ComEscopo_VeSomenteOsDadosDasSuasEscolas()
    {
        var seed = Seed();
        using var context = CreateContext(seed.Database, escolas: [seed.EscolaA]);

        Assert.Equal(["Escola A"], context.Escolas.Select(e => e.Nome).ToList());
        Assert.Equal(["Turma A"], context.Turmas.Select(t => t.NomeCompleto).ToList());
        Assert.Equal(["Aluno A"], context.Alunos.Select(a => a.Nome).ToList());
        Assert.Single(context.AlunosTurmas.ToList());
        Assert.Single(context.Chamadas.ToList());
        Assert.Single(context.ChamadasAlunos.ToList());
        Assert.Single(context.ProfessorAlocacoes.ToList());
        Assert.Equal(["Professor A"], context.Professores.Select(p => p.Nome).ToList());

        // Cadastros da rede continuam compartilhados.
        Assert.Single(context.ModalidadesEnsino.ToList());
    }

    [Fact]
    public void ProfessorDeVariasEscolas_ApareceParaQuemAcessaQualquerUmaDelas()
    {
        var seed = Seed(professorAAtuaNasDuas: true);
        using var context = CreateContext(seed.Database, escolas: [seed.EscolaB]);

        Assert.Equal(["Professor A", "Professor B"], context.Professores.Select(p => p.Nome).OrderBy(n => n).ToList());
    }

    [Fact]
    public async Task ChecagensDaInstituicao_IgnoramOEscopo()
    {
        var seed = Seed();
        using var context = CreateContext(seed.Database, escolas: [seed.EscolaA]);
        var alunos = new AlunoRepository(context);

        // Aluno da escola B, fora do escopo: CPF e sequência de matrícula valem para a rede toda.
        // (Professor e INEP usam ILike, que só existe no PostgreSQL.)
        Assert.True(await alunos.ExistsByCpfAsync("39053344705"));
        Assert.Equal(2, await alunos.GetMaxSequencialMatriculaAsync(2026));
        Assert.NotNull(await new AlunoTurmaRepository(context).GetAtivaByAlunoIdAsync(
            context.Alunos.IgnoreQueryFilters([AppDbContext.FiltroEscola]).Single(a => a.Nome == "Aluno B").Id));
    }

    [Fact]
    public async Task Resolver_PerfilComEscolas_RestringeAsEscolasDoPerfil()
    {
        var seed = Seed();
        using var context = CreateContext(seed.Database, escolas: null);
        AdicionarUsuario(context, seed, Perfil.Diretor, 10, escolas: [seed.EscolaB]);

        var escopo = await new EscopoEscolaResolver(context).ResolverAsync(10);

        Assert.Equal([seed.EscolaB], escopo);
    }

    [Fact]
    public async Task Resolver_PerfilDaRedeToda_NaoRestringe()
    {
        var seed = Seed();
        using var context = CreateContext(seed.Database, escolas: null);
        AdicionarUsuario(context, seed, Perfil.Gerencia, 10, escolas: []);

        Assert.Null(await new EscopoEscolaResolver(context).ResolverAsync(10));
    }

    [Fact]
    public async Task Resolver_ProfessorSemEscolasNoPerfil_UsaAsEscolasEmQueLeciona()
    {
        var seed = Seed();
        using var context = CreateContext(seed.Database, escolas: null);
        AdicionarUsuario(context, seed, Perfil.Professor, 10, escolas: []);
        var professor = context.Professores.Single(p => p.Nome == "Professor B");
        professor.UsuarioId = 10;
        context.SaveChanges();

        var escopo = await new EscopoEscolaResolver(context).ResolverAsync(10);

        Assert.Equal([seed.EscolaB], escopo);
    }

    private static void AdicionarUsuario(AppDbContext context, SeedResult seed, string perfilNome, int usuarioId, int[] escolas)
    {
        var perfil = context.Perfis.SingleOrDefault(p => p.Nome == perfilNome) ?? new Perfil { Nome = perfilNome };
        context.Users.Add(new User { Id = usuarioId, TenantId = Tenant, Email = $"{usuarioId}@x.com", Cpf = $"{usuarioId:D11}" });
        if (escolas.Length == 0)
            context.UsuariosPerfis.Add(new UsuarioPerfil { UsuarioId = usuarioId, Perfil = perfil });
        foreach (var escola in escolas)
            context.UsuariosPerfis.Add(new UsuarioPerfil { UsuarioId = usuarioId, Perfil = perfil, EscolaId = escola });
        context.SaveChanges();
    }

    private sealed record SeedResult(string Database, int EscolaA, int EscolaB);

    private static SeedResult Seed(bool professorAAtuaNasDuas = false)
    {
        var database = Guid.NewGuid().ToString();
        using var system = CreateContext(database, escolas: null, tenantId: null);

        system.Tenants.Add(new Tenant { Id = Tenant, Nome = "Rede", Slug = "rede" });
        var modalidade = new ModalidadeEnsino { TenantId = Tenant, Nome = "Fundamental", Sigla = "EF" };
        var etapa = new EtapaEnsino { TenantId = Tenant, ModalidadeEnsino = modalidade, Nome = "1º ano", Sigla = "1A" };
        var ano = new AnoLetivo { TenantId = Tenant, AnoReferencia = 2026, TipoPeriodo = AnoLetivo.TipoBimestral };
        var disciplina = new Disciplina { TenantId = Tenant, Nome = "Matemática", Codigo = "MAT" };
        system.AddRange(modalidade, etapa, ano, disciplina);

        var escolas = new[] { "A", "B" }.Select((sufixo, i) => new Escola
        {
            TenantId = Tenant,
            Nome = $"Escola {sufixo}",
            CodigoInep = $"{i + 1}{i + 1}{i + 1}{i + 1}{i + 1}{i + 1}{i + 1}{i + 1}",
        }).ToArray();
        system.Escolas.AddRange(escolas);

        var cpfs = new[] { "52998224725", "39053344705" };
        var professores = new List<Professor>();
        for (var i = 0; i < escolas.Length; i++)
        {
            var sufixo = i == 0 ? "A" : "B";
            var turma = new Turma
            {
                TenantId = Tenant, AnoLetivo = ano, Escola = escolas[i], ModalidadeEnsino = modalidade, EtapaEnsino = etapa,
                NomeIdentificador = sufixo, NomeCompleto = $"Turma {sufixo}", VagasOfertadas = 30,
            };
            var aluno = new Aluno
            {
                TenantId = Tenant, Escola = escolas[i], Nome = $"Aluno {sufixo}", Matricula = $"2026000{i + 1}", CpfAluno = cpfs[i],
            };
            var professor = new Professor { TenantId = Tenant, Nome = $"Professor {sufixo}", Cpf = i == 0 ? "11144477735" : "71428793860" };
            professor.ProfessorEscolas.Add(new ProfessorEscola { TenantId = Tenant, Escola = escolas[i] });
            if (professorAAtuaNasDuas && i == 1)
                professores[0].ProfessorEscolas.Add(new ProfessorEscola { TenantId = Tenant, Escola = escolas[i] });
            professores.Add(professor);

            system.AddRange(turma, aluno, professor);
            system.AlunosTurmas.Add(new AlunoTurma { TenantId = Tenant, Aluno = aluno, Turma = turma, DataInicio = new DateOnly(2026, 2, 1) });
            system.ProfessorAlocacoes.Add(new ProfessorAlocacao { TenantId = Tenant, Professor = professor, Turma = turma, Disciplina = disciplina });
            var chamada = new Chamada { TenantId = Tenant, Turma = turma, Disciplina = disciplina, Data = new DateOnly(2026, 3, 2) };
            chamada.Registros.Add(new ChamadaAluno { TenantId = Tenant, Aluno = aluno });
            system.Chamadas.Add(chamada);
        }

        system.SaveChanges();
        return new SeedResult(database, escolas[0].Id, escolas[1].Id);
    }

    private static AppDbContext CreateContext(string database, IReadOnlyList<int>? escolas, int? tenantId = Tenant)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(database)
            .Options;

        return new AppDbContext(options, new FixedTenantContext(tenantId, escolas));
    }

    private sealed class FixedTenantContext(int? tenantId, IReadOnlyList<int>? escolaIds) : ITenantContext
    {
        public int? TenantId { get; } = tenantId;
        public string? TenantSlug => TenantId?.ToString();
        public IReadOnlyList<int>? EscolaIds { get; } = escolaIds;
    }
}
