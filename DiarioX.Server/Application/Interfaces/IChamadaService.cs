using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Chamadas;

namespace DiarioX.Server.Application.Interfaces;

public interface IChamadaService
{
    /// <summary>Turmas e disciplinas em que o usuário pode lançar chamada (o professor vê só as suas alocações).</summary>
    Task<IEnumerable<ChamadaTurmaResponse>> GetTurmasAsync(UsuarioAtual usuario);

    /// <summary>Chamada da data: a já registrada ou a lista de alunos enturmados para lançar.</summary>
    Task<ChamadaQueryResult<ChamadaResponse>> GetAsync(UsuarioAtual usuario, int turmaId, int disciplinaId, DateOnly data);

    Task<ChamadaQueryResult<IEnumerable<ChamadaResumoResponse>>> ListAsync(UsuarioAtual usuario, int turmaId, int disciplinaId);

    Task<ChamadaQueryResult<FrequenciaResponse>> GetFrequenciaAsync(UsuarioAtual usuario, int turmaId, int disciplinaId, int? periodoId);

    Task<ChamadaCommandResult> CreateAsync(UsuarioAtual usuario, ChamadaRequest request);
    Task<ChamadaCommandResult> UpdateAsync(UsuarioAtual usuario, int id, ChamadaRequest request);
    Task<ChamadaCommandResult> DeleteAsync(UsuarioAtual usuario, int id);
}
