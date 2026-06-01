using DiarioX.Server.Application.DTOs.Turmas;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class TurmaServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsMappedResponses()
    {
        var (service, turmaRepo, _, _, _, _) = BuildService();
        turmaRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { BuildEntity(1), BuildEntity(2) });

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("3º Ano A - Ensino Fundamental - Manhã", result[0].NomeCompleto);
    }

    [Fact]
    public async Task CreateAsync_WhenRequiredFieldsMissing_ReturnsValidationError()
    {
        var (service, _, _, _, _, _) = BuildService();
        var request = BuildValidRequest();
        request.EscolaId = 0;

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(TurmaResultError.Validation, result.Error);
        Assert.Equal("Por favor, preencha todos os campos obrigatórios.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenVagasNotPositive_ReturnsValidationError()
    {
        var (service, _, _, _, _, _) = BuildService();
        var request = BuildValidRequest();
        request.VagasOfertadas = 0;

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(TurmaResultError.Validation, result.Error);
        Assert.Equal("Vagas ofertadas deve ser maior que zero.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenEtapaNotInModalidade_ReturnsValidationError()
    {
        var (service, turmaRepo, anoRepo, escolaRepo, modalidadeRepo, etapaRepo) = BuildService();
        SetupBaseDependencies(anoRepo, escolaRepo, modalidadeRepo, etapaRepo);
        turmaRepo
            .Setup(r => r.ExistsByCombinacaoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(false);

        etapaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new EtapaEnsino
        {
            Id = 1,
            Nome = "3º Ano",
            Sigla = "3ANO",
            OrdemCronologica = 3,
            ModalidadeEnsinoId = 999,
            ModalidadeEnsino = new ModalidadeEnsino
            {
                Id = 999,
                Nome = "Outra Modalidade",
                Sigla = "OM",
                Status = ModalidadeEnsino.StatusAtivo
            }
        });

        var result = await service.CreateAsync(BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(TurmaResultError.Validation, result.Error);
        Assert.Equal("A etapa de ensino selecionada não pertence à modalidade informada.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateExists_ReturnsConflict()
    {
        var (service, turmaRepo, anoRepo, escolaRepo, modalidadeRepo, etapaRepo) = BuildService();
        SetupBaseDependencies(anoRepo, escolaRepo, modalidadeRepo, etapaRepo);
        turmaRepo
            .Setup(r => r.ExistsByCombinacaoAsync(1, 1, 1, "Turma A", Turma.TurnoManha, null))
            .ReturnsAsync(true);

        var result = await service.CreateAsync(BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(TurmaResultError.Conflict, result.Error);
        Assert.Equal("Já existe uma turma cadastrada com essas mesmas características para esta escola.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_CreatesTurmaWithGeneratedNomeCompletoAndStatusAtivo()
    {
        var (service, turmaRepo, anoRepo, escolaRepo, modalidadeRepo, etapaRepo) = BuildService();
        SetupBaseDependencies(anoRepo, escolaRepo, modalidadeRepo, etapaRepo);
        turmaRepo
            .Setup(r => r.ExistsByCombinacaoAsync(1, 1, 1, "Turma A", Turma.TurnoManha, null))
            .ReturnsAsync(false);

        Turma? captured = null;
        turmaRepo
            .Setup(r => r.AddAsync(It.IsAny<Turma>()))
            .Callback<Turma>(t => captured = t)
            .ReturnsAsync((Turma t) => { t.Id = 10; return t; });
        turmaRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(BuildEntity(10));

        var result = await service.CreateAsync(BuildValidRequest());

        Assert.True(result.Success);
        Assert.Equal("Turma cadastrada com sucesso!", result.Message);
        Assert.NotNull(result.Turma);
        Assert.Equal(10, result.Turma!.Id);

        Assert.NotNull(captured);
        Assert.Equal(Turma.StatusAtivo, captured!.Status);
        Assert.Equal("3º Ano A - Ensino Fundamental - Manhã", captured.NomeCompleto);
        Assert.Equal("Turma A", captured.NomeIdentificador);
    }

    private static (TurmaService Service,
        Mock<ITurmaRepository> TurmaRepo,
        Mock<IAnoLetivoRepository> AnoRepo,
        Mock<IEscolaRepository> EscolaRepo,
        Mock<IModalidadeEnsinoRepository> ModalidadeRepo,
        Mock<IEtapaEnsinoRepository> EtapaRepo) BuildService()
    {
        var turmaRepo = new Mock<ITurmaRepository>();
        var anoRepo = new Mock<IAnoLetivoRepository>();
        var escolaRepo = new Mock<IEscolaRepository>();
        var modalidadeRepo = new Mock<IModalidadeEnsinoRepository>();
        var etapaRepo = new Mock<IEtapaEnsinoRepository>();

        var service = new TurmaService(
            turmaRepo.Object,
            anoRepo.Object,
            escolaRepo.Object,
            modalidadeRepo.Object,
            etapaRepo.Object);

        return (service, turmaRepo, anoRepo, escolaRepo, modalidadeRepo, etapaRepo);
    }

    private static void SetupBaseDependencies(
        Mock<IAnoLetivoRepository> anoRepo,
        Mock<IEscolaRepository> escolaRepo,
        Mock<IModalidadeEnsinoRepository> modalidadeRepo,
        Mock<IEtapaEnsinoRepository> etapaRepo)
    {
        anoRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new AnoLetivo
        {
            Id = 1,
            AnoReferencia = 2026,
            DataInicio = new DateOnly(2026, 2, 1),
            DataTermino = new DateOnly(2026, 12, 15),
            TipoPeriodo = AnoLetivo.TipoBimestral
        });

        escolaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Escola
        {
            Id = 1,
            Nome = "Escola Municipal Modelo",
            CodigoInep = "12345",
            Cnpj = "12345678000199",
            Telefone = "11999999999",
            EmailInstitucional = "contato@escola.local",
            Municipio = "São Paulo",
            EnderecoCompleto = "Rua A",
            Status = Escola.StatusAtivo
        });

        modalidadeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ModalidadeEnsino
        {
            Id = 1,
            Nome = "Ensino Fundamental",
            Sigla = "EF",
            Status = ModalidadeEnsino.StatusAtivo
        });

        etapaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new EtapaEnsino
        {
            Id = 1,
            ModalidadeEnsinoId = 1,
            Nome = "3º Ano",
            Sigla = "3ANO",
            OrdemCronologica = 3,
            ModalidadeEnsino = new ModalidadeEnsino
            {
                Id = 1,
                Nome = "Ensino Fundamental",
                Sigla = "EF",
                Status = ModalidadeEnsino.StatusAtivo
            }
        });
    }

    private static TurmaRequest BuildValidRequest() => new()
    {
        AnoLetivoId = 1,
        EscolaId = 1,
        ModalidadeEnsinoId = 1,
        EtapaEnsinoId = 1,
        NomeIdentificador = "Turma A",
        Turno = Turma.TurnoManha,
        VagasOfertadas = 35,
    };

    private static Turma BuildEntity(int id) => new()
    {
        Id = id,
        AnoLetivoId = 1,
        AnoLetivo = new AnoLetivo
        {
            Id = 1,
            AnoReferencia = 2026,
            DataInicio = new DateOnly(2026, 2, 1),
            DataTermino = new DateOnly(2026, 12, 15),
            TipoPeriodo = AnoLetivo.TipoBimestral
        },
        EscolaId = 1,
        Escola = new Escola
        {
            Id = 1,
            Nome = "Escola Municipal Modelo",
            CodigoInep = "12345",
            Cnpj = "12345678000199",
            Telefone = "11999999999",
            EmailInstitucional = "contato@escola.local",
            Municipio = "São Paulo",
            EnderecoCompleto = "Rua A",
            Status = Escola.StatusAtivo
        },
        ModalidadeEnsinoId = 1,
        ModalidadeEnsino = new ModalidadeEnsino
        {
            Id = 1,
            Nome = "Ensino Fundamental",
            Sigla = "EF",
            Status = ModalidadeEnsino.StatusAtivo
        },
        EtapaEnsinoId = 1,
        EtapaEnsino = new EtapaEnsino
        {
            Id = 1,
            ModalidadeEnsinoId = 1,
            Nome = "3º Ano",
            Sigla = "3ANO",
            OrdemCronologica = 3,
            ModalidadeEnsino = new ModalidadeEnsino
            {
                Id = 1,
                Nome = "Ensino Fundamental",
                Sigla = "EF",
                Status = ModalidadeEnsino.StatusAtivo
            }
        },
        NomeIdentificador = "Turma A",
        NomeCompleto = "3º Ano A - Ensino Fundamental - Manhã",
        Turno = Turma.TurnoManha,
        VagasOfertadas = 35,
        Status = Turma.StatusAtivo,
    };
}