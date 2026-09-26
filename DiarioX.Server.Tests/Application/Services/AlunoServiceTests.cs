using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class AlunoServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsMappedResponses()
    {
        var fixture = BuildService();
        fixture.AlunoRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new[]
            {
                BuildAluno(1, "20260001"),
                BuildAluno(2, "20260002")
            });

        var result = (await fixture.Service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("20260001", result[0].Matricula);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        var fixture = BuildService();
        fixture.AlunoRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Aluno?)null);

        var result = await fixture.Service.GetByIdAsync(10);

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.NotFound, result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var fixture = BuildService();
        fixture.AlunoRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildAluno(1, "20260001"));

        var result = await fixture.Service.GetByIdAsync(1);

        Assert.True(result.Success);
        Assert.Equal("20260001", result.Aluno!.Matricula);
    }

    [Fact]
    public async Task CreateAsync_WhenNomeMissing_ReturnsValidationError()
    {
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.Nome = "  ";

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("Nome completo é obrigatório.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenSexoInvalido_ReturnsValidationError()
    {
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.Sexo = "OUTRO";

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("Sexo/Gênero é obrigatório.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenCpfResponsavelInvalido_ReturnsValidationError()
    {
        // EX03
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.ResponsavelCpf1 = "12345678900";

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("CPF inválido.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenMaiorDeIdadeSemCpf_ReturnsValidationError()
    {
        // RN03
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.DataNascimento = new DateTime(2000, 1, 1);
        request.CpfAluno = null;

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("CPF do aluno é obrigatório para alunos maiores de 18 anos.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenMenorSemCpfSemCertidao_ReturnsValidationError()
    {
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.CpfAluno = null;
        request.CertidaoNascimento = null;

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("Certidão de nascimento é obrigatória quando o aluno não possui CPF.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenEscolaNaoEncontrada_ReturnsDependencyNotFound()
    {
        var fixture = BuildService();
        var request = BuildValidRequest();
        fixture.EscolaRepository.Setup(r => r.GetByIdAsync(request.EscolaId)).ReturnsAsync((Escola?)null);

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.DependencyNotFound, result.Error);
        Assert.Equal("Escola não encontrada.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenCpfAlunoDuplicado_ReturnsConflict()
    {
        // RN01/EX02 - chave por CPF
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.CpfAluno = "52998224725";
        fixture.EscolaRepository.Setup(r => r.GetByIdAsync(request.EscolaId)).ReturnsAsync(BuildEscola(1));
        fixture.AlunoRepository.Setup(r => r.ExistsByCpfAsync("52998224725", null)).ReturnsAsync(true);

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Conflict, result.Error);
        Assert.Equal("Atenção: Já existe um aluno cadastrado no sistema com estes dados.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenComboSemCpfDuplicado_ReturnsConflict()
    {
        // RN01/EX02 - chave combinada (sem CPF)
        var fixture = BuildService();
        var request = BuildValidRequest();
        fixture.EscolaRepository.Setup(r => r.GetByIdAsync(request.EscolaId)).ReturnsAsync(BuildEscola(1));
        fixture.AlunoRepository
            .Setup(r => r.ExistsByNomeDataNascimentoResponsavelAsync(request.Nome, request.DataNascimento, request.ResponsavelNome1, null))
            .ReturnsAsync(true);

        var result = await fixture.Service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Conflict, result.Error);
        Assert.Equal("Atenção: Já existe um aluno cadastrado no sistema com estes dados.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_GeneratesMatriculaAndPersists()
    {
        // RN02
        var fixture = BuildService();
        var request = BuildValidRequest();
        var anoAtual = DateTime.UtcNow.Year;

        fixture.EscolaRepository.Setup(r => r.GetByIdAsync(request.EscolaId)).ReturnsAsync(BuildEscola(1));
        fixture.AlunoRepository.Setup(r => r.GetMaxSequencialMatriculaAsync(anoAtual)).ReturnsAsync(5);
        fixture.AlunoRepository.Setup(r => r.ExistsByMatriculaAsync(It.IsAny<string>())).ReturnsAsync(false);

        Aluno? added = null;
        fixture.AlunoRepository
            .Setup(r => r.AddAsync(It.IsAny<Aluno>()))
            .Callback<Aluno>(a => added = a)
            .ReturnsAsync((Aluno a) => { a.Id = 50; return a; });

        fixture.AlunoRepository
            .Setup(r => r.GetByIdAsync(50))
            .ReturnsAsync(() => added is null ? null : BuildAluno(50, added.Matricula));

        var result = await fixture.Service.CreateAsync(request);

        Assert.True(result.Success);
        Assert.Equal($"{anoAtual}0006", result.Aluno!.Matricula);
        Assert.Equal(Aluno.StatusAtivoAguardandoEnturmacao, result.Aluno.Status);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFound()
    {
        var fixture = BuildService();
        fixture.AlunoRepository.Setup(r => r.GetByIdAsync(90)).ReturnsAsync((Aluno?)null);

        var result = await fixture.Service.UpdateAsync(90, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_DoesNotChangeMatricula()
    {
        // RN02 - a matrícula é imutável
        var fixture = BuildService();
        var request = BuildValidRequest();
        request.Nome = "Nome Atualizado";

        var existing = BuildAluno(4, "20260099");
        fixture.AlunoRepository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(existing);
        fixture.EscolaRepository.Setup(r => r.GetByIdAsync(request.EscolaId)).ReturnsAsync(BuildEscola(1));

        var result = await fixture.Service.UpdateAsync(4, request);

        Assert.True(result.Success);
        Assert.Equal("20260099", existing.Matricula);
        Assert.Equal("Nome Atualizado", existing.Nome);
        fixture.AlunoRepository.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFound()
    {
        var fixture = BuildService();
        fixture.AlunoRepository.Setup(r => r.GetByIdAsync(101)).ReturnsAsync((Aluno?)null);

        var result = await fixture.Service.DeleteAsync(101);

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.NotFound, result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsSuccess()
    {
        var fixture = BuildService();
        var aluno = BuildAluno(5, "20260005");
        fixture.AlunoRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(aluno);

        var result = await fixture.Service.DeleteAsync(5);

        Assert.True(result.Success);
        fixture.AlunoRepository.Verify(r => r.DeleteAsync(aluno), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenPossuiEnturmacao_ReturnsConflictAndDoesNotDelete()
    {
        var fixture = BuildService();
        var aluno = BuildAluno(5, "20260005");
        fixture.AlunoRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(aluno);
        fixture.AlunoTurmaRepository.Setup(r => r.ExistsByAlunoIdAsync(5)).ReturnsAsync(true);

        var result = await fixture.Service.DeleteAsync(5);

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Conflict, result.Error);
        fixture.AlunoRepository.Verify(r => r.DeleteAsync(It.IsAny<Aluno>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenInativando_SetsStatusInativo()
    {
        var fixture = BuildService();
        var aluno = BuildAluno(5, "20260001");
        fixture.AlunoRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(aluno);

        var result = await fixture.Service.UpdateStatusAsync(5, new AlunoStatusRequest { Status = " inativo " });

        Assert.True(result.Success);
        Assert.Equal("Aluno inativado com sucesso!", result.Message);
        Assert.Equal(Aluno.StatusInativo, aluno.Status);
        fixture.AlunoRepository.Verify(r => r.UpdateAsync(aluno), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenAtivandoSemEnturmacao_SetsAguardandoEnturmacao()
    {
        var fixture = BuildService();
        var aluno = BuildAluno(5, "20260001");
        aluno.Status = Aluno.StatusInativo;
        fixture.AlunoRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(aluno);
        fixture.AlunoTurmaRepository.Setup(r => r.GetAtivaByAlunoIdAsync(5)).ReturnsAsync((AlunoTurma?)null);

        var result = await fixture.Service.UpdateStatusAsync(5, new AlunoStatusRequest { Status = Aluno.StatusAtivo });

        Assert.True(result.Success);
        Assert.Equal(Aluno.StatusAtivoAguardandoEnturmacao, aluno.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenAtivandoComEnturmacaoAtiva_SetsAtivo()
    {
        var fixture = BuildService();
        var aluno = BuildAluno(5, "20260001");
        aluno.Status = Aluno.StatusInativo;
        fixture.AlunoRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(aluno);
        fixture.AlunoTurmaRepository.Setup(r => r.GetAtivaByAlunoIdAsync(5)).ReturnsAsync(new AlunoTurma { AlunoId = 5, TurmaId = 1 });

        var result = await fixture.Service.UpdateStatusAsync(5, new AlunoStatusRequest { Status = Aluno.StatusAtivo });

        Assert.True(result.Success);
        Assert.Equal(Aluno.StatusAtivo, aluno.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenStatusInvalido_ReturnsValidationError()
    {
        var fixture = BuildService();

        var result = await fixture.Service.UpdateStatusAsync(5, new AlunoStatusRequest { Status = "ATIVO_AGUARDANDO_ENTURMACAO" });

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        fixture.AlunoRepository.Verify(r => r.UpdateAsync(It.IsAny<Aluno>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenNotFound_ReturnsNotFound()
    {
        var fixture = BuildService();
        fixture.AlunoRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Aluno?)null);

        var result = await fixture.Service.UpdateStatusAsync(99, new AlunoStatusRequest { Status = Aluno.StatusInativo });

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.NotFound, result.Error);
    }

    private static (AlunoService Service, Mock<IAlunoRepository> AlunoRepository, Mock<IEscolaRepository> EscolaRepository, Mock<IAlunoTurmaRepository> AlunoTurmaRepository) BuildService()
    {
        var alunoRepository = new Mock<IAlunoRepository>();
        var escolaRepository = new Mock<IEscolaRepository>();
        var alunoTurmaRepository = new Mock<IAlunoTurmaRepository>();

        var service = new AlunoService(alunoRepository.Object, escolaRepository.Object, alunoTurmaRepository.Object);
        return (service, alunoRepository, escolaRepository, alunoTurmaRepository);
    }

    private static AlunoRequest BuildValidRequest() => new()
    {
        Nome = "Maria da Silva",
        DataNascimento = new DateTime(2015, 4, 10),
        Sexo = "FEMININO",
        CorRaca = "PARDA",
        NecessidadeEspecial = false,
        ResponsavelNome1 = "Joana da Silva",
        ResponsavelCpf1 = "52998224725",
        ResponsavelTelefone1 = "11999998888",
        ResponsavelNome2 = null,
        CpfAluno = null,
        CertidaoNascimento = "123456 01 55 2015 1 00001 001 0000001-11",
        Cep = "01310200",
        EnderecoCompleto = "Av. Paulista",
        Numero = "1000",
        Bairro = "Bela Vista",
        EscolaId = 1,
    };

    private static Escola BuildEscola(int id) => new()
    {
        Id = id,
        Nome = "Escola Modelo",
        CodigoInep = "12345678",
        Cnpj = "11111111000191",
        Telefone = "(11) 99999-0000",
        EmailInstitucional = "contato@escola.com",
        Municipio = "Sao Paulo",
        EnderecoCompleto = "Rua A, 123",
        Status = Escola.StatusAtivo,
    };

    private static Aluno BuildAluno(int id, string matricula) => new()
    {
        Id = id,
        Matricula = matricula,
        Nome = "Maria da Silva",
        DataNascimento = new DateTime(2015, 4, 10),
        Sexo = Aluno.SexoFeminino,
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
        Escola = BuildEscola(1),
        Status = Aluno.StatusAtivoAguardandoEnturmacao,
    };
}
