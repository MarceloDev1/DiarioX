using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Conteudos;

namespace DiarioX.Server.Application.Interfaces;

/// <remarks>
/// O conteúdo é sempre por turma, disciplina e dia. Nas turmas de frequência diária (Anos Iniciais, RF017 RN02)
/// a chamada não tem disciplina, então o diário confere a frequência do dia contra os conteúdos de todas as
/// disciplinas da turma.
/// </remarks>
public interface IConteudoMinistradoService
{
    /// <summary>Turmas e disciplinas em que o usuário pode registrar conteúdo (o professor vê só as suas alocações).</summary>
    Task<IEnumerable<ConteudoTurmaResponse>> GetTurmasAsync(UsuarioAtual usuario);

    /// <summary>Conteúdo da data: o já registrado ou o formulário em branco (com o bloqueio do calendário, se houver).</summary>
    Task<ConteudoQueryResult<ConteudoMinistradoResponse>> GetAsync(UsuarioAtual usuario, int turmaId, int? disciplinaId, DateOnly data);

    /// <summary>RN01: dias com frequência ou conteúdo, sinalizando os que têm só um dos dois.</summary>
    Task<ConteudoQueryResult<DiarioResponse>> GetDiarioAsync(UsuarioAtual usuario, int turmaId, int? disciplinaId, DateOnly? de, DateOnly? ate);

    /// <summary>RN02: habilidades da BNCC cadastradas para a etapa da turma e a disciplina.</summary>
    Task<ConteudoQueryResult<IEnumerable<HabilidadeResumoResponse>>> GetSugestoesAsync(UsuarioAtual usuario, int turmaId, int? disciplinaId, string? busca);

    Task<ConteudoCommandResult> CreateAsync(UsuarioAtual usuario, ConteudoMinistradoRequest request);
    Task<ConteudoCommandResult> UpdateAsync(UsuarioAtual usuario, int id, ConteudoMinistradoRequest request);
    Task<ConteudoCommandResult> DeleteAsync(UsuarioAtual usuario, int id);
}
