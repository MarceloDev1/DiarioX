using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Notas;

namespace DiarioX.Server.Application.Interfaces;

public interface INotaService
{
    /// <summary>Turmas, disciplinas e períodos em que o usuário pode lançar ou consultar notas.</summary>
    Task<IEnumerable<NotaTurmaResponse>> GetTurmasAsync(UsuarioAtual usuario);

    Task<NotaResult<NotasPeriodoResponse>> GetPeriodoAsync(UsuarioAtual usuario, int turmaId, int disciplinaId, int periodoId);
    Task<NotaResult<MediasResponse>> GetMediasAsync(UsuarioAtual usuario, int turmaId, int disciplinaId);

    /// <summary>Cria (id nulo) ou altera uma avaliação.</summary>
    Task<NotaResult<AvaliacaoResponse>> SalvarAvaliacaoAsync(UsuarioAtual usuario, int? id, AvaliacaoRequest request);
    Task<NotaResult<bool>> ExcluirAvaliacaoAsync(UsuarioAtual usuario, int id);

    Task<NotaResult<NotasPeriodoResponse>> LancarNotasAsync(UsuarioAtual usuario, LancamentoNotasRequest request);

    /// <summary>Médias de todos os alunos em todas as disciplinas da turma (relatório; sem escopo de professor).</summary>
    Task<NotaResult<MapaNotasResponse>> GetMapaDaTurmaAsync(int turmaId);
}
