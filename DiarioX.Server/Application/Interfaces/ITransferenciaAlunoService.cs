using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Alunos;

namespace DiarioX.Server.Application.Interfaces;

public interface ITransferenciaAlunoService
{
    Task<IReadOnlyList<TransferenciaResponse>> GetByAlunoIdAsync(int alunoId);
    Task<TransferenciaResult> TransferirAsync(UsuarioAtual usuario, int alunoId, TransferenciaRequest request);

    /// <summary>PDF da Declaração de Transferência; nulo se a transferência não existir.</summary>
    Task<DeclaracaoTransferenciaArquivo?> GerarDeclaracaoAsync(int transferenciaId);
}
