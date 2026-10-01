namespace DiarioX.Server.Application.Transferencias;

/// <summary>Dados impressos na Declaração de Transferência (RF014).</summary>
public sealed record DeclaracaoTransferenciaDados(
    string? Instituicao,
    string EscolaNome,
    string? EscolaInep,
    string? EscolaMunicipio,
    string? EscolaEndereco,
    string? EscolaTelefone,
    string AlunoNome,
    string Matricula,
    DateTime DataNascimento,
    string? Responsavel,
    string? Turma,
    int AnoReferencia,
    DateOnly DataTransferencia,
    string TipoDescricao,
    string EscolaDestino,
    string? Motivo,
    DateTime EmitidaEm);

public interface IDeclaracaoTransferenciaPdf
{
    byte[] Gerar(DeclaracaoTransferenciaDados dados);
}
