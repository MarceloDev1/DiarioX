using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IChamadaRepository
{
    Task<Chamada?> GetByIdAsync(int id);
    Task<Chamada?> GetAsync(int turmaId, int disciplinaId, DateOnly data);

    /// <summary>Chamadas da turma/disciplina (com os registros), opcionalmente dentro de um intervalo de datas.</summary>
    Task<IReadOnlyList<Chamada>> ListAsync(int turmaId, int disciplinaId, DateOnly? de = null, DateOnly? ate = null);

    /// <summary>Enturmações da turma que se sobrepõem ao intervalo (com o aluno).</summary>
    Task<IReadOnlyList<AlunoTurma>> GetEnturmacoesAsync(int turmaId, DateOnly de, DateOnly ate);

    /// <summary>E-mail dos usuários que registraram/atualizaram chamadas (inclui o Administrador global).</summary>
    Task<IReadOnlyDictionary<int, string>> GetEmailsUsuariosAsync(IEnumerable<int> usuarioIds);

    Task<Chamada> AddAsync(Chamada chamada);
    Task UpdateAsync(Chamada chamada, IEnumerable<ChamadaAluno> registros);
    Task DeleteAsync(Chamada chamada);
}
