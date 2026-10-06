using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Alunos;

namespace DiarioX.Server.Application.Interfaces;

public interface ITransferenciaAlunoService
{
    /// <summary>Alunos transferidos, uma linha por transferência, da mais recente para a mais antiga.</summary>
    Task<IReadOnlyList<TransferenciaListaItemResponse>> ListAsync();
    Task<IReadOnlyList<TransferenciaResponse>> GetByAlunoIdAsync(int alunoId);
    Task<TransferenciaResult> TransferirAsync(UsuarioAtual usuario, int alunoId, TransferenciaRequest request);

    /// <summary>PDF da Declaração de Transferência; nulo se a transferência não existir.</summary>
    Task<DeclaracaoTransferenciaArquivo?> GerarDeclaracaoAsync(int transferenciaId);
}
