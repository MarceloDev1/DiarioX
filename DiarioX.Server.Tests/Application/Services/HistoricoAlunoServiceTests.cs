using DiarioX.Server.Application.Alunos;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class HistoricoAlunoServiceTests
{
    private const int EscolaA = 6, EscolaB = 7;
    private readonly Mock<IHistoricoAlunoConsultas> _consultas = new();

    [Fact]
    public async Task AlunoNaoEncontrado_RetornaNulo()
    {
        var historico = await new HistoricoAlunoService(_consultas.Object).ObterAsync(1);

        Assert.Null(historico);
    }

    [Fact]
    public async Task TransferenciaEntreEscolasDaRede_MostraTransferidoNaOrigemEAguardandoNoDestino()
    {
        ComDados(
            escolaId: EscolaB, escola: "EM B", status: Aluno.StatusAtivoAguardandoEnturmacao,
            vinculos:
            [
                new(1, "1º ANO A", EscolaA, "EM A", new(2026, 2, 2), new(2026, 5, 3), AlunoTurma.MotivoRemanejamento, "Pedido da família"),
                new(2, "1º ANO B", EscolaA, "EM A", new(2026, 5, 4), new(2026, 8, 9), AlunoTurma.MotivoTransferencia, null),
            ],
            transferencias:
            [
                new(1, new(2026, 8, 10), Transferencia.TipoEntreEscolasDaRede, EscolaA, "EM A", EscolaB, "EM B", "1º ANO B", "Mudança de endereço"),
            ]);

        var historico = await new HistoricoAlunoService(_consultas.Object).ObterAsync(1);

        Assert.NotNull(historico);
        Assert.Collection(historico!.Escolas,
            e => { Assert.Equal("EM A", e.Escola); Assert.Equal(Aluno.StatusTransferido, e.Situacao); Assert.False(e.Atual); },
            e => { Assert.Equal("EM B", e.Escola); Assert.Equal(Aluno.StatusAtivoAguardandoEnturmacao, e.Situacao); Assert.True(e.Atual); });

        Assert.Equal(
            [
                HistoricoAlunoService.TipoCadastro,
                HistoricoAlunoService.TipoEnturmacao,
                HistoricoAlunoService.TipoRemanejamento,
                HistoricoAlunoService.TipoTransferenciaSaida,
                HistoricoAlunoService.TipoTransferenciaEntrada,
            ],
            historico.Movimentacoes.Select(m => m.Tipo));

        var cadastro = historico.Movimentacoes[0];
        Assert.Equal("EM A", cadastro.Escola);

        var remanejamento = historico.Movimentacoes[2];
        Assert.Equal(new DateOnly(2026, 5, 4), remanejamento.Data);
        Assert.Equal("Remanejamento da turma 1º ANO A para a turma 1º ANO B", remanejamento.Descricao);
        Assert.Equal("Pedido da família", remanejamento.Observacao);

        var saida = historico.Movimentacoes[3];
        Assert.Equal("EM A", saida.Escola);
        Assert.Equal(Aluno.StatusTransferido, saida.Situacao);
        Assert.Equal("Transferência Entre Escolas da Rede para EM B", saida.Descricao);

        var entrada = historico.Movimentacoes[4];
        Assert.Equal("EM B", entrada.Escola);
        Assert.Equal(Aluno.StatusAtivoAguardandoEnturmacao, entrada.Situacao);
    }

    [Fact]
    public async Task Desenturmacao_UsaODiaSeguinteAoUltimoNaTurmaEOStatusDoMotivo()
    {
        ComDados(
            escolaId: EscolaA, escola: "EM A", status: Aluno.StatusNaoCompareceu,
            vinculos: [new(1, "1º ANO A", EscolaA, "EM A", new(2026, 2, 2), new(2026, 3, 9), AlunoTurma.MotivoNaoCompareceu, null)],
            transferencias: []);

        var historico = await new HistoricoAlunoService(_consultas.Object).ObterAsync(1);

        var desenturmacao = historico!.Movimentacoes[^1];
        Assert.Equal(HistoricoAlunoService.TipoDesenturmacao, desenturmacao.Tipo);
        Assert.Equal(new DateOnly(2026, 3, 10), desenturmacao.Data);
        Assert.Equal(Aluno.StatusNaoCompareceu, desenturmacao.Situacao);
        Assert.Equal("Desenturmação da turma 1º ANO A — Não comparecimento", desenturmacao.Descricao);
        Assert.Single(historico.Escolas);
    }

    [Fact]
    public async Task TransferenciaParaOutraRede_NaoListaEscolaDeDestino()
    {
        ComDados(
            escolaId: EscolaA, escola: "EM A", status: Aluno.StatusTransferido,
            vinculos: [],
            transferencias: [new(1, new(2026, 4, 1), Transferencia.TipoOutraRede, EscolaA, "EM A", null, "Colégio Y", null, null)]);

        var historico = await new HistoricoAlunoService(_consultas.Object).ObterAsync(1);

        var escola = Assert.Single(historico!.Escolas);
        Assert.Equal(Aluno.StatusTransferido, escola.Situacao);
        Assert.DoesNotContain(historico.Movimentacoes, m => m.Tipo == HistoricoAlunoService.TipoTransferenciaEntrada);
    }

    private void ComDados(int escolaId, string escola, string status,
        IReadOnlyList<VinculoHistoricoLinha> vinculos, IReadOnlyList<TransferenciaHistoricoLinha> transferencias)
        => _consultas.Setup(c => c.ObterAsync(1)).ReturnsAsync(new HistoricoAlunoDados(
            1, "Carla Mendes", "20260001", new DateTime(2026, 1, 20), escolaId, escola, status, vinculos, transferencias));
}
