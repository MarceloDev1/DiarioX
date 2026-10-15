using DiarioX.Server.Application.DTOs.Alunos;

namespace DiarioX.Server.Application.Interfaces;

public interface IHistoricoAlunoService
{
    /// <summary>Nulo se o aluno não existir ou nunca tiver passado por uma escola do escopo do usuário.</summary>
    Task<HistoricoAlunoResponse?> ObterAsync(int alunoId);
}
