using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class DesenturmacaoServiceTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public async Task DesenturmarAsync_SemAlunos_ReturnsValidation()
    {
        var (service, alunoTurmaRepository) = BuildService();

        var result = await service.DesenturmarAsync(Request(alunoIds: []));

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("Selecione ao menos um aluno para realizar a desenturmação.", result.Message);
        VerifyNothingPersisted(alunoTurmaRepository);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task DesenturmarAsync_SemMotivo_ReturnsValidation(string? motivo)
    {
        var (service, alunoTurmaRepository) = BuildService();

        var result = await service.DesenturmarAsync(Request(motivo: motivo));

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("Por favor, selecione o motivo da desenturmação para continuar.", result.Message);
        VerifyNothingPersisted(alunoTurmaRepository);
    }

    [Fact]
    public async Task DesenturmarAsync_MotivoDesconhecido_ReturnsValidation()
    {
        var (service, alunoTurmaRepository) = BuildService();

        var result = await service.DesenturmarAsync(Request(motivo: "TRANSFERENCIA"));

        Assert.False(result.Success);
        Assert.Equal("Motivo da desenturmação inválido.", result.Message);
        VerifyNothingPersisted(alunoTurmaRepository);
    }

    [Fact]
    public async Task DesenturmarAsync_OutrosSemObservacao_ReturnsValidation()
    {
        var (service, alunoTurmaRepository) = BuildService();

        var result = await service.DesenturmarAsync(Request(motivo: AlunoTurma.MotivoOutros, observacao: " "));

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("Informe a observação/justificativa quando o motivo for \"Outros\".", result.Message);
        VerifyNothingPersisted(alunoTurmaRepository);
    }

    [Fact]
    public async Task DesenturmarAsync_AlunoForaDaTurma_ListaFalhasENaoGravaNada()
    {
        var (service, alunoTurmaRepository) = BuildService(BuildAluno(1), BuildAluno(2));

        var result = await service.DesenturmarAsync(Request(alunoIds: [1, 7, 8]));

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("2 alunos não estão enturmados nesta turma. Nenhuma desenturmação foi realizada.", result.Message);
        Assert.Equal(
        [
            new DesenturmacaoFalha(7, "Aluno não está enturmado nesta turma."),
            new DesenturmacaoFalha(8, "Aluno não está enturmado nesta turma."),
        ], result.Falhas);
        VerifyNothingPersisted(alunoTurmaRepository);
    }

    [Fact]
    public async Task DesenturmarAsync_TurmaInexistente_ReturnsNotFound()
    {
        var (service, alunoTurmaRepository) = BuildService();

        var result = await service.DesenturmarAsync(Request(turmaId: 99));

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.NotFound, result.Error);
        VerifyNothingPersisted(alunoTurmaRepository);
    }

    [Theory]
    [InlineData(AlunoTurma.MotivoReestruturacaoInterna, Aluno.StatusAtivoAguardandoEnturmacao)]
    [InlineData(AlunoTurma.MotivoErroMatricula, Aluno.StatusAtivoAguardandoEnturmacao)]
    [InlineData(AlunoTurma.MotivoOutros, Aluno.StatusAtivoAguardandoEnturmacao)]
    [InlineData(AlunoTurma.MotivoNaoCompareceu, Aluno.StatusNaoCompareceu)]
    [InlineData(AlunoTurma.MotivoFalecimento, Aluno.StatusInativoObito)]
    public async Task DesenturmarAsync_StatusDoAlunoSegueOMotivo(string motivo, string statusEsperado)
    {
        var (service, alunoTurmaRepository) = BuildService(BuildAluno(1), BuildAluno(2));

        var result = await service.DesenturmarAsync(Request(alunoIds: [1], motivo: motivo.ToLowerInvariant(), observacao: " Mudou de cidade "));

        Assert.True(result.Success);
        Assert.Equal("Aluno desenturmado com sucesso!", result.Message);
        alunoTurmaRepository.Verify(x => x.DesenturmarAsync(
            1,
            It.Is<IReadOnlyDictionary<int, string>>(s => s.Count == 1 && s[1] == statusEsperado),
            Hoje,
            motivo,
            "Mudou de cidade"), Times.Once);
    }

    [Fact]
    public async Task DesenturmarAsync_AlunoInativoVoltandoParaAguardando_ContinuaInativo()
    {
        var (service, alunoTurmaRepository) = BuildService(BuildAluno(1, Aluno.StatusInativo), BuildAluno(2));

        var result = await service.DesenturmarAsync(Request(alunoIds: [1, 2], motivo: AlunoTurma.MotivoReestruturacaoInterna));

        Assert.True(result.Success);
        alunoTurmaRepository.Verify(x => x.DesenturmarAsync(
            1,
            It.Is<IReadOnlyDictionary<int, string>>(s =>
                s[1] == Aluno.StatusInativo && s[2] == Aluno.StatusAtivoAguardandoEnturmacao),
            Hoje,
            AlunoTurma.MotivoReestruturacaoInterna,
            null), Times.Once);
    }

    [Fact]
    public async Task DesenturmarAsync_TurmaInteira_InformaQueTodosForamDesenturmados()
    {
        var (service, _) = BuildService(BuildAluno(1), BuildAluno(2), BuildAluno(3));

        var result = await service.DesenturmarAsync(Request(alunoIds: [3, 1, 2, 2]));

        Assert.True(result.Success);
        Assert.Equal("Todos os alunos da turma foram desenturmados com sucesso!", result.Message);
    }

    [Fact]
    public async Task DesenturmarAsync_ParteDaTurma_InformaQuantidade()
    {
        var (service, _) = BuildService(BuildAluno(1), BuildAluno(2), BuildAluno(3));

        var result = await service.DesenturmarAsync(Request(alunoIds: [1, 2]));

        Assert.True(result.Success);
        Assert.Equal("2 alunos desenturmados com sucesso!", result.Message);
    }

    [Fact]
    public async Task DesenturmarAsync_QuandoAlunoSaiDaTurmaDuranteAGravacao_ReturnsConflict()
    {
        var (service, alunoTurmaRepository) = BuildService(BuildAluno(1));
        alunoTurmaRepository
            .Setup(x => x.DesenturmarAsync(It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<int, string>>(), It.IsAny<DateOnly>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("O aluno não está mais enturmado nesta turma."));

        var result = await service.DesenturmarAsync(Request(alunoIds: [1]));

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Conflict, result.Error);
        Assert.Equal("O aluno não está mais enturmado nesta turma.", result.Message);
    }

    private static DesenturmacaoRequest Request(
        List<int>? alunoIds = null,
        string? motivo = AlunoTurma.MotivoErroMatricula,
        string? observacao = null,
        int turmaId = 1) => new()
    {
        TurmaId = turmaId,
        AlunoIds = alunoIds ?? [1],
        Motivo = motivo,
        Observacao = observacao,
    };

    private static void VerifyNothingPersisted(Mock<IAlunoTurmaRepository> alunoTurmaRepository)
        => alunoTurmaRepository.Verify(x => x.DesenturmarAsync(It.IsAny<int>(), It.IsAny<IReadOnlyDictionary<int, string>>(),
            It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);

    /// <summary>Serviço com a turma 1 contendo os alunos informados.</summary>
    private static (RemanejamentoAlunoService Service, Mock<IAlunoTurmaRepository> AlunoTurmaRepository) BuildService(params Aluno[] enturmados)
    {
        var alunoTurmaRepository = new Mock<IAlunoTurmaRepository>();
        var turmaRepository = new Mock<ITurmaRepository>();
        turmaRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Turma { Id = 1, EscolaId = 1, NomeCompleto = "Turma 1" });
        alunoTurmaRepository.Setup(x => x.GetAtivasByTurmaIdAsync(1)).ReturnsAsync(enturmados
            .Select(aluno => new AlunoTurma { AlunoId = aluno.Id, Aluno = aluno, TurmaId = 1, DataInicio = Hoje.AddDays(-30) })
            .ToList());

        var service = new RemanejamentoAlunoService(new Mock<IAlunoRepository>().Object, alunoTurmaRepository.Object, turmaRepository.Object);
        return (service, alunoTurmaRepository);
    }

    private static Aluno BuildAluno(int id, string status = Aluno.StatusAtivo)
        => new() { Id = id, Nome = $"Aluno {id}", Matricula = $"2026{id:0000}", EscolaId = 1, Status = status };
}
