using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class RemanejamentoAlunoService : IRemanejamentoAlunoService
{
    private readonly IAlunoTurmaRepository _alunoTurmaRepository;
    private readonly ITurmaRepository _turmaRepository;

    public RemanejamentoAlunoService(IAlunoTurmaRepository alunoTurmaRepository, ITurmaRepository turmaRepository)
    {
        _alunoTurmaRepository = alunoTurmaRepository;
        _turmaRepository = turmaRepository;
    }

    public async Task<EnturmacaoAtivaResponse?> GetEnturmacaoAtivaAsync(int alunoId)
    {
        var vinculo = await _alunoTurmaRepository.GetAtivaByAlunoIdAsync(alunoId);
        return vinculo is null ? null : new EnturmacaoAtivaResponse(
            vinculo.AlunoId,
            vinculo.Aluno.EscolaId,
            vinculo.Aluno.Escola.Nome,
            vinculo.TurmaId,
            vinculo.Turma.NomeCompleto,
            vinculo.Turma.AnoLetivoId,
            vinculo.Turma.AnoLetivo.AnoReferencia,
            vinculo.Turma.Turno,
            vinculo.DataInicio);
    }

    public async Task<RemanejamentoAlunoResult> RemanejarAsync(int alunoId, RemanejamentoAlunoRequest request)
    {
        if (alunoId <= 0 || request.TurmaDestinoId <= 0 || request.DataMovimentacao == default)
            return Invalid("Por favor, informe a data da movimentação e a nova turma.");

        var vinculoOrigem = await _alunoTurmaRepository.GetAtivaByAlunoIdAsync(alunoId);
        if (vinculoOrigem is null)
            return new(false, "O aluno não possui enturmação ativa.", AlunoResultError.NotFound);

        var turmaDestino = await _turmaRepository.GetByIdAsync(request.TurmaDestinoId);
        if (turmaDestino is null)
            return new(false, "Turma de destino não encontrada.", AlunoResultError.NotFound);

        if (turmaDestino.Id == vinculoOrigem.TurmaId)
            return Invalid("A turma de destino deve ser diferente da turma atual do aluno.");

        if (turmaDestino.Status != Turma.StatusAtivo ||
            turmaDestino.EscolaId != vinculoOrigem.Turma.EscolaId ||
            turmaDestino.AnoLetivoId != vinculoOrigem.Turma.AnoLetivoId)
        {
            return Invalid("A turma de destino deve pertencer à mesma escola e ao mesmo ano letivo da turma atual.");
        }

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        if (request.DataMovimentacao < vinculoOrigem.Turma.AnoLetivo.DataInicio || request.DataMovimentacao > hoje)
            return Invalid("A data da movimentação deve estar entre o início do ano letivo e a data atual.");

        if (request.DataMovimentacao <= vinculoOrigem.DataInicio)
            return Invalid("A data da movimentação deve ser posterior ao início da enturmação atual.");

        if (!await _alunoTurmaRepository.HasVacancyAsync(turmaDestino.Id, request.DataMovimentacao))
            return new(false, "A turma de destino não possui vagas disponíveis para remanejamento.", AlunoResultError.Conflict);

        try
        {
            await _alunoTurmaRepository.RemanejarAsync(vinculoOrigem, turmaDestino.Id, request.DataMovimentacao);
        }
        catch (InvalidOperationException)
        {
            return new(false, "A turma de destino não possui vagas disponíveis para remanejamento.", AlunoResultError.Conflict);
        }

        return new(true, $"Aluno remanejado com sucesso para a turma {turmaDestino.NomeCompleto}!");
    }

    private static RemanejamentoAlunoResult Invalid(string message)
        => new(false, message, AlunoResultError.Validation);
}