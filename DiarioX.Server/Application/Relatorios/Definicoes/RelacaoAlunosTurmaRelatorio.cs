using DiarioX.Server.Domain.Entities;
using static DiarioX.Server.Application.Relatorios.FormatacaoRelatorio;

namespace DiarioX.Server.Application.Relatorios.Definicoes;

/// <summary>Relação nominal dos alunos enturmados na turma, com o contato do responsável.</summary>
public class RelacaoAlunosTurmaRelatorio : IRelatorio
{
    private readonly IRelatorioConsultas _consultas;

    public RelacaoAlunosTurmaRelatorio(IRelatorioConsultas consultas)
    {
        _consultas = consultas;
    }

    public string Id => "relacao-alunos-turma";
    public string Nome => "Relação de alunos da turma";
    public string Descricao => "Lista nominal dos alunos enturmados, com idade e contato do responsável.";
    public string Categoria => "Secretaria";
    public IReadOnlyList<string> Permissoes { get; } = [Auth.Permissoes.Alunos.Visualizar];
    public bool Paisagem => true;

    public IReadOnlyList<FiltroDefinicao> Filtros { get; } =
    [
        new(FiltrosRelatorio.AnoLetivo, "Ano letivo"),
        new(FiltrosRelatorio.Escola, "Escola"),
        new(FiltrosRelatorio.Turma, "Turma", Obrigatorio: true),
    ];

    public async Task<RelatorioResultado<ConteudoRelatorio>> GerarAsync(RelatorioFiltros filtros, DateOnly hoje)
    {
        if (filtros.TurmaId is not int turmaId)
            return new(default, "Selecione a turma.", RelatorioErro.Validation);

        var turma = await _consultas.ObterTurmaAsync(turmaId);
        if (turma is null)
            return new(default, "Turma não encontrada.", RelatorioErro.NotFound);

        var alunos = await _consultas.ListarAlunosDaTurmaAsync(turmaId, hoje);

        var linhas = alunos
            .Select((a, i) => new object?[]
            {
                i + 1,
                a.Matricula,
                a.Nome,
                DateOnly.FromDateTime(a.DataNascimento),
                Idade(a.DataNascimento, hoje),
                Sexo(a.Sexo),
                a.Responsavel,
                Telefone(a.TelefoneResponsavel),
                a.EnturmadoEm,
            })
            .ToList();

        return new(new ConteudoRelatorio(
            [
                new("Turma", turma.Nome),
                new("Escola", turma.Escola),
                new("Ano letivo", turma.AnoReferencia.ToString()),
                new("Posição em", hoje.ToString("dd/MM/yyyy", PtBr)),
            ],
            [
                new("Nº", TiposColuna.Inteiro),
                new("Matrícula"),
                new("Aluno"),
                new("Nascimento", TiposColuna.Data),
                new("Idade", TiposColuna.Inteiro),
                new("Sexo"),
                new("Responsável"),
                new("Telefone do responsável"),
                new("Enturmado em", TiposColuna.Data),
            ],
            linhas,
            [
                new("Alunos", Inteiro(alunos.Count)),
                new("Feminino", Inteiro(alunos.Count(a => a.Sexo == Aluno.SexoFeminino))),
                new("Masculino", Inteiro(alunos.Count(a => a.Sexo == Aluno.SexoMasculino))),
                new("Com necessidade especial", Inteiro(alunos.Count(a => a.NecessidadeEspecial))),
            ]));
    }
}
