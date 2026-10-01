namespace DiarioX.Server.Application.DTOs.Alunos;

/// <summary>Aluno com enturmação ativa na turma.</summary>
public sealed record AlunoEnturmadoResponse(int AlunoId, string Matricula, string Nome, string Status, DateOnly DataInicio);

/// <summary>Enturmação ativa com os dados da turma, para a lista de alunos enturmados.</summary>
public sealed record EnturmacaoAtivaItemResponse(
    int AlunoId,
    string Matricula,
    string Nome,
    string Status,
    DateOnly DataInicio,
    int TurmaId,
    string TurmaNomeIdentificador,
    string TurmaNomeCompleto,
    int EscolaId,
    string EscolaNome,
    int ModalidadeEnsinoId,
    string ModalidadeEnsinoNome,
    int EtapaEnsinoId,
    string EtapaEnsinoNome,
    string Turno);
