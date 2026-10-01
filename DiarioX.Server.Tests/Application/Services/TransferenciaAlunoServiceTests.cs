using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Application.Transferencias;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class TransferenciaAlunoServiceTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);
    private static readonly UsuarioAtual Secretaria = new(UsuarioId: 3, IsGlobalAdmin: false);
    private const string MensagemDataInvalida =
        "A data de transferência deve estar dentro do período do Ano Letivo vigente e não pode ser uma data futura.";

    [Fact]
    public async Task TransferirAsync_AlunoInexistente_ReturnsNotFound()
    {
        var f = new Fixture();

        var result = await f.Service.TransferirAsync(Secretaria, 99, Request());

        Assert.Equal(AlunoResultError.NotFound, result.Error);
        f.VerifyNadaGravado();
    }

    [Fact]
    public async Task TransferirAsync_AlunoJaTransferido_ExibeAvisoEX02()
    {
        var f = new Fixture(status: Aluno.StatusTransferido);

        var result = await f.Service.TransferirAsync(Secretaria, Fixture.AlunoId, Request());

        Assert.False(result.Success);
        Assert.Equal(AlunoResultError.Conflict, result.Error);
        Assert.Equal("Este aluno já possui o status de Transferido no sistema.", result.Message);
        f.VerifyNadaGravado();
    }

    [Theory]
    [InlineData(Aluno.StatusInativo)]
    [InlineData(Aluno.StatusInativoObito)]
    [InlineData(Aluno.StatusNaoCompareceu)]
    public async Task TransferirAsync_AlunoForaDasSituacoesPermitidas_ReturnsValidation(string status)
    {
        var f = new Fixture(status: status);

        var result = await f.Service.TransferirAsync(Secretaria, Fixture.AlunoId, Request());

        Assert.Equal(AlunoResultError.Validation, result.Error);
        Assert.Equal("Somente alunos matriculados ou aguardando enturmação podem ser transferidos.", result.Message);
        f.VerifyNadaGravado();
    }

    [Theory]
    [InlineData("", "Escola Estadual X", "Selecione o tipo de transferência.")]
    [InlineData("PARA_O_EXTERIOR", "Escola Estadual X", "Selecione o tipo de transferência.")]
    [InlineData(Transferencia.TipoOutraRede, "   ", "Informe a escola de destino.")]
    public async Task TransferirAsync_CamposObrigatorios_ReturnsValidation(string tipo, string escolaDestino, string mensagem)
    {
        var f = new Fixture();

        var result = await f.Service.TransferirAsync(Secretaria, Fixture.AlunoId, Request(tipo: tipo, escolaDestino: escolaDestino));

        Assert.Equal(mensagem, result.Message);
        f.VerifyNadaGravado();
    }

    [Fact]
    public async Task TransferirAsync_DataFutura_ExibeMensagemEX01()
    {
        var f = new Fixture();

        var result = await f.Service.TransferirAsync(Secretaria, Fixture.AlunoId, Request(data: Hoje.AddDays(1)));

        Assert.Equal(MensagemDataInvalida, result.Message);
        f.VerifyNadaGravado();
    }

    [Fact]
    public async Task TransferirAsync_DataAntesDoAnoLetivo_ExibeMensagemEX01()
    {
        var f = new Fixture();

        var result = await f.Service.TransferirAsync(Secretaria, Fixture.AlunoId, Request(data: f.AnoLetivo.DataInicio.AddDays(-1)));

        Assert.Equal(MensagemDataInvalida, result.Message);
        f.VerifyNadaGravado();
    }

    [Fact]
    public async Task TransferirAsync_DataAntesDaEnturmacaoAtual_ReturnsValidation()
    {
        var f = new Fixture(inicioEnturmacao: Hoje.AddDays(-5));

        var result = await f.Service.TransferirAsync(Secretaria, Fixture.AlunoId, Request(data: Hoje.AddDays(-10)));

        Assert.Equal($"A data de transferência não pode ser anterior ao início da enturmação atual ({Hoje.AddDays(-5):dd/MM/yyyy}).", result.Message);
        f.VerifyNadaGravado();
    }

    [Fact]
    public async Task TransferirAsync_AlunoEnturmado_EncerraTurmaNaEscolaDaTurma()
    {
        var f = new Fixture();
        Transferencia? gravada = null;
        f.Transferencias.Setup(r => r.TransferirAsync(It.IsAny<Transferencia>(), Fixture.VinculoId))
            .Callback<Transferencia, int?>((t, _) => { t.Id = 50; gravada = t; })
            .ReturnsAsync((Transferencia t, int? _) => t);

        var result = await f.Service.TransferirAsync(Secretaria, Fixture.AlunoId,
            Request(tipo: " mudanca_municipio_estado ", escolaDestino: "  EE Rio Branco  ", motivo: "  "));

        Assert.True(result.Success, result.Message);
        Assert.Equal("Transferência realizada com sucesso!", result.Message);
        Assert.NotNull(gravada);
        Assert.Equal(Fixture.EscolaDaTurmaId, gravada!.EscolaOrigemId);
        Assert.Equal(Fixture.TurmaId, gravada.TurmaId);
        Assert.Equal(f.AnoLetivo.Id, gravada.AnoLetivoId);
        Assert.Equal(Transferencia.TipoMudancaMunicipioEstado, gravada.Tipo);
        Assert.Equal("EE Rio Branco", gravada.EscolaDestino);
        Assert.Null(gravada.Motivo);
        Assert.Equal(Secretaria.UsuarioId, gravada.RegistradoPorUsuarioId);
    }

    [Fact]
    public async Task TransferirAsync_AlunoAguardandoEnturmacao_UsaAnoLetivoVigenteEEscolaDoCadastro()
    {
        var f = new Fixture(status: Aluno.StatusAtivoAguardandoEnturmacao, enturmado: false);
        Transferencia? gravada = null;
        f.Transferencias.Setup(r => r.TransferirAsync(It.IsAny<Transferencia>(), null))
            .Callback<Transferencia, int?>((t, _) => gravada = t)
            .ReturnsAsync((Transferencia t, int? _) => t);

        var result = await f.Service.TransferirAsync(Secretaria, Fixture.AlunoId, Request());

        Assert.True(result.Success, result.Message);
        Assert.Equal(Fixture.EscolaDoCadastroId, gravada!.EscolaOrigemId);
        Assert.Null(gravada.TurmaId);
        Assert.Equal(f.AnoLetivo.Id, gravada.AnoLetivoId);
    }

    [Fact]
    public async Task TransferirAsync_AguardandoSemAnoLetivoVigente_ReturnsValidation()
    {
        var f = new Fixture(status: Aluno.StatusAtivoAguardandoEnturmacao, enturmado: false, anoVigente: false);

        var result = await f.Service.TransferirAsync(Secretaria, Fixture.AlunoId, Request());

        Assert.Equal("Não há ano letivo vigente cadastrado para registrar a transferência.", result.Message);
        f.VerifyNadaGravado();
    }

    [Fact]
    public async Task TransferirAsync_QuandoSituacaoMudaDuranteAGravacao_ReturnsConflict()
    {
        var f = new Fixture();
        f.Transferencias.Setup(r => r.TransferirAsync(It.IsAny<Transferencia>(), It.IsAny<int?>()))
            .ThrowsAsync(new InvalidOperationException("Este aluno já possui o status de Transferido no sistema."));

        var result = await f.Service.TransferirAsync(Secretaria, Fixture.AlunoId, Request());

        Assert.Equal(AlunoResultError.Conflict, result.Error);
        Assert.Equal("Este aluno já possui o status de Transferido no sistema.", result.Message);
    }

    [Fact]
    public async Task GerarDeclaracaoAsync_MontaOsDadosDaDeclaracao()
    {
        var f = new Fixture();
        f.Transferencias.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(new Transferencia
        {
            Id = 50,
            Aluno = f.Aluno,
            EscolaOrigem = new Escola { Nome = "EM Monteiro Lobato", CodigoInep = "12345678", Municipio = "Campinas" },
            Turma = new Turma { NomeCompleto = "1º Ano A" },
            AnoLetivo = f.AnoLetivo,
            DataTransferencia = Hoje,
            Tipo = Transferencia.TipoOutraRede,
            EscolaDestino = "Colégio Particular Y",
        });

        var arquivo = await f.Service.GerarDeclaracaoAsync(50);

        Assert.NotNull(arquivo);
        Assert.Equal("declaracao-transferencia-20260001.pdf", arquivo!.NomeArquivo);
        f.Pdf.Verify(p => p.Gerar(It.Is<DeclaracaoTransferenciaDados>(d =>
            d.Instituicao == "Rede de Teste" &&
            d.EscolaNome == "EM Monteiro Lobato" &&
            d.AlunoNome == "Carla Mendes" &&
            d.Turma == "1º Ano A" &&
            d.TipoDescricao == "Transferência para Outra Rede" &&
            d.EscolaDestino == "Colégio Particular Y" &&
            d.EscolaTelefone == null)), Times.Once);
    }

    private static TransferenciaRequest Request(
        DateOnly? data = null,
        string tipo = Transferencia.TipoOutraRede,
        string escolaDestino = "Escola Estadual X",
        string? motivo = null) => new()
    {
        DataTransferencia = data ?? Hoje,
        Tipo = tipo,
        EscolaDestino = escolaDestino,
        Motivo = motivo,
    };

    private sealed class Fixture
    {
        public const int AlunoId = 1, TurmaId = 10, VinculoId = 77, EscolaDaTurmaId = 6, EscolaDoCadastroId = 6;

        public Mock<ITransferenciaRepository> Transferencias { get; } = new();
        public Mock<IDeclaracaoTransferenciaPdf> Pdf { get; } = new();
        public AnoLetivo AnoLetivo { get; } = new()
        {
            Id = 3, AnoReferencia = Hoje.Year, DataInicio = Hoje.AddDays(-100), DataTermino = Hoje.AddDays(100),
        };
        public Aluno Aluno { get; }
        public TransferenciaAlunoService Service { get; }

        public Fixture(string status = Aluno.StatusAtivo, bool enturmado = true, bool anoVigente = true, DateOnly? inicioEnturmacao = null)
        {
            Aluno = new Aluno { Id = AlunoId, Nome = "Carla Mendes", Matricula = "20260001", EscolaId = EscolaDoCadastroId, Status = status };

            var alunos = new Mock<IAlunoRepository>();
            alunos.Setup(r => r.GetByIdAsync(AlunoId)).ReturnsAsync(Aluno);

            var alunoTurmas = new Mock<IAlunoTurmaRepository>();
            if (enturmado)
            {
                alunoTurmas.Setup(r => r.GetAtivaByAlunoIdAsync(AlunoId)).ReturnsAsync(new AlunoTurma
                {
                    Id = VinculoId, AlunoId = AlunoId, Aluno = Aluno, TurmaId = TurmaId,
                    DataInicio = inicioEnturmacao ?? AnoLetivo.DataInicio,
                    Turma = new Turma { Id = TurmaId, EscolaId = EscolaDaTurmaId, AnoLetivoId = AnoLetivo.Id, AnoLetivo = AnoLetivo },
                });
            }

            var anos = new Mock<IAnoLetivoRepository>();
            anos.Setup(r => r.GetVigenteAsync(Hoje)).ReturnsAsync(anoVigente ? AnoLetivo : null);

            var tenants = new Mock<ITenantRepository>();
            tenants.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Tenant { Id = 1, Nome = "Rede de Teste" });
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(c => c.TenantId).Returns(1);

            Pdf.Setup(p => p.Gerar(It.IsAny<DeclaracaoTransferenciaDados>())).Returns([0x25, 0x50, 0x44, 0x46]);
            Transferencias.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Transferencia?)null);

            Service = new TransferenciaAlunoService(alunos.Object, alunoTurmas.Object, anos.Object, Transferencias.Object,
                tenants.Object, tenantContext.Object, Pdf.Object);
        }

        public void VerifyNadaGravado()
            => Transferencias.Verify(r => r.TransferirAsync(It.IsAny<Transferencia>(), It.IsAny<int?>()), Times.Never);
    }
}
