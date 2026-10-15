using DiarioX.Server.Application.Alunos;
using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Application.Services;

/// <summary>Monta o histórico de movimentação do aluno a partir das enturmações e transferências.</summary>
public class HistoricoAlunoService : IHistoricoAlunoService
{
    public const string TipoCadastro = "CADASTRO";
    public const string TipoEnturmacao = "ENTURMACAO";
    public const string TipoRemanejamento = "REMANEJAMENTO";
    public const string TipoDesenturmacao = "DESENTURMACAO";
    public const string TipoTransferenciaSaida = "TRANSFERENCIA_SAIDA";
    public const string TipoTransferenciaEntrada = "TRANSFERENCIA_ENTRADA";

    private static readonly IReadOnlyDictionary<string, string> MotivosDesenturmacao = new Dictionary<string, string>
    {
        [AlunoTurma.MotivoReestruturacaoInterna] = "Reestruturação interna",
        [AlunoTurma.MotivoNaoCompareceu] = "Não comparecimento",
        [AlunoTurma.MotivoFalecimento] = "Falecimento",
        [AlunoTurma.MotivoErroMatricula] = "Erro de matrícula/enturmação",
        [AlunoTurma.MotivoOutros] = "Outros",
    };

    // No mesmo dia, a saída de uma escola vem antes da entrada na outra e da enturmação nela.
    private static readonly IReadOnlyDictionary<string, int> OrdemNoDia = new Dictionary<string, int>
    {
        [TipoDesenturmacao] = 0,
        [TipoTransferenciaSaida] = 1,
        [TipoTransferenciaEntrada] = 2,
        [TipoEnturmacao] = 3,
        [TipoRemanejamento] = 3,
    };

    private readonly IHistoricoAlunoConsultas _consultas;

    public HistoricoAlunoService(IHistoricoAlunoConsultas consultas)
    {
        _consultas = consultas;
    }

    public async Task<HistoricoAlunoResponse?> ObterAsync(int alunoId)
    {
        var dados = alunoId > 0 ? await _consultas.ObterAsync(alunoId) : null;
        return dados is null ? null : new HistoricoAlunoResponse(
            dados.AlunoId, dados.Nome, dados.Matricula, SituacaoPorEscola(dados), Movimentacoes(dados));
    }

    /// <summary>A escola atual com o status do aluno; as escolas de onde ele saiu transferido, como TRANSFERIDO.</summary>
    private static List<SituacaoEscolaResponse> SituacaoPorEscola(HistoricoAlunoDados dados)
    {
        var escolas = new List<(int Id, string Nome)>();
        void Adicionar(int id, string nome)
        {
            if (escolas.All(e => e.Id != id))
                escolas.Add((id, nome));
        }

        foreach (var t in dados.Transferencias)
        {
            Adicionar(t.EscolaOrigemId, t.EscolaOrigem);
            if (t.EscolaDestinoId is int destinoId)
                Adicionar(destinoId, t.EscolaDestino);
        }
        Adicionar(dados.EscolaId, dados.EscolaNome);

        return escolas
            .Select(e => e.Id == dados.EscolaId
                ? new SituacaoEscolaResponse(e.Id, e.Nome, dados.Status, true)
                : new SituacaoEscolaResponse(e.Id, e.Nome, Aluno.StatusTransferido, false))
            .ToList();
    }

    private static List<MovimentacaoAlunoResponse> Movimentacoes(HistoricoAlunoDados dados)
    {
        var eventos = new List<MovimentacaoAlunoResponse>();

        // Um remanejamento encerra o vínculo na véspera e abre o novo na data da movimentação.
        var remanejadoPara = new Dictionary<int, VinculoHistoricoLinha>();
        foreach (var v in dados.Vinculos.Where(v => v.Motivo == AlunoTurma.MotivoRemanejamento && v.DataFim is not null))
        {
            var sucessor = dados.Vinculos.FirstOrDefault(x =>
                x.Id > v.Id && x.DataInicio == v.DataFim!.Value.AddDays(1) && !remanejadoPara.ContainsKey(x.Id));
            if (sucessor is not null)
                remanejadoPara[sucessor.Id] = v;
        }

        foreach (var v in dados.Vinculos)
        {
            eventos.Add(remanejadoPara.TryGetValue(v.Id, out var anterior)
                ? new MovimentacaoAlunoResponse(v.DataInicio, TipoRemanejamento,
                    $"Remanejamento da turma {anterior.Turma} para a turma {v.Turma}",
                    v.Escola, v.Turma, Aluno.StatusAtivo, anterior.Observacao)
                : new MovimentacaoAlunoResponse(v.DataInicio, TipoEnturmacao,
                    $"Enturmação na turma {v.Turma}", v.Escola, v.Turma, Aluno.StatusAtivo, null));

            // Fins por remanejamento e transferência aparecem nos próprios eventos.
            if (v.DataFim is not DateOnly fim || v.Motivo is AlunoTurma.MotivoRemanejamento or AlunoTurma.MotivoTransferencia)
                continue;

            // O dia da saída é o seguinte ao último na turma; o vínculo desfeito no dia em que começaria
            // termina na véspera do início.
            var saida = fim.AddDays(1) > v.DataInicio ? fim.AddDays(1) : v.DataInicio;
            var motivo = v.Motivo is null ? null : MotivosDesenturmacao.GetValueOrDefault(v.Motivo, v.Motivo);
            eventos.Add(new MovimentacaoAlunoResponse(saida, TipoDesenturmacao,
                motivo is null ? $"Desenturmação da turma {v.Turma}" : $"Desenturmação da turma {v.Turma} — {motivo}",
                v.Escola, v.Turma, StatusAposDesenturmacao(v.Motivo), v.Observacao));
        }

        foreach (var t in dados.Transferencias)
        {
            var tipo = Transferencia.Tipos.GetValueOrDefault(t.Tipo, t.Tipo);
            eventos.Add(new MovimentacaoAlunoResponse(t.Data, TipoTransferenciaSaida,
                $"{tipo} para {t.EscolaDestino}", t.EscolaOrigem, t.Turma, Aluno.StatusTransferido, t.Motivo));

            if (t.EscolaDestinoId is not null)
                eventos.Add(new MovimentacaoAlunoResponse(t.Data, TipoTransferenciaEntrada,
                    $"Recebido(a) por transferência da escola {t.EscolaOrigem}", t.EscolaDestino, null,
                    Aluno.StatusAtivoAguardandoEnturmacao, null));
        }

        // O cadastro abre o histórico mesmo quando a enturmação foi lançada com data anterior a ele.
        var cadastro = new MovimentacaoAlunoResponse(DateOnly.FromDateTime(dados.CreatedAt), TipoCadastro,
            $"Cadastro do aluno (matrícula {dados.Matricula})", EscolaInicial(dados), null,
            Aluno.StatusAtivoAguardandoEnturmacao, null);

        return eventos
            .Select((e, i) => (Evento: e, Indice: i))
            .OrderBy(x => x.Evento.Data)
            .ThenBy(x => OrdemNoDia[x.Evento.Tipo])
            .ThenBy(x => x.Indice)
            .Select(x => x.Evento)
            .Prepend(cadastro)
            .ToList();
    }

    private static string EscolaInicial(HistoricoAlunoDados dados)
        => dados.Transferencias.FirstOrDefault()?.EscolaOrigem
           ?? dados.Vinculos.FirstOrDefault()?.Escola
           ?? dados.EscolaNome;

    private static string StatusAposDesenturmacao(string? motivo) => motivo switch
    {
        AlunoTurma.MotivoFalecimento => Aluno.StatusInativoObito,
        AlunoTurma.MotivoNaoCompareceu => Aluno.StatusNaoCompareceu,
        _ => Aluno.StatusAtivoAguardandoEnturmacao,
    };
}
