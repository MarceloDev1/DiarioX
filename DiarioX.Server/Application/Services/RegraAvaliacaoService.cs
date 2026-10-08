using DiarioX.Server.Application.DTOs.RegrasAvaliacao;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class RegraAvaliacaoService : IRegraAvaliacaoService
{
    private const int MaxNome = 100;

    private readonly IRegraAvaliacaoRepository _repository;
    private readonly IEtapaEnsinoRepository _etapaRepository;

    public RegraAvaliacaoService(IRegraAvaliacaoRepository repository, IEtapaEnsinoRepository etapaRepository)
    {
        _repository = repository;
        _etapaRepository = etapaRepository;
    }

    public async Task<IEnumerable<RegraAvaliacaoResponse>> GetAllAsync()
        => (await _repository.GetAllAsync()).Select(MapToResponse);

    public async Task<RegraAvaliacaoResponse?> GetByIdAsync(int id)
    {
        var regra = await _repository.GetByIdAsync(id);
        return regra is null ? null : MapToResponse(regra);
    }

    public RegraAvaliacaoResponse GetPadrao() => MapToResponse(RegraAvaliacao.PadraoDoSistema());

    public async Task<RegraAvaliacaoCommandResult> CreateAsync(RegraAvaliacaoRequest request)
    {
        var (regra, etapaIds, erro) = await ValidarAsync(request, null);
        if (erro is not null)
            return erro;

        var criada = await _repository.AddAsync(regra!, etapaIds!);
        var salva = await _repository.GetByIdAsync(criada.Id);
        return new RegraAvaliacaoCommandResult(true, "Regra de avaliação cadastrada com sucesso!", MapToResponse(salva!));
    }

    public async Task<RegraAvaliacaoCommandResult> UpdateAsync(int id, RegraAvaliacaoRequest request)
    {
        if (await _repository.GetByIdAsync(id) is null)
            return NotFound();

        var (regra, etapaIds, erro) = await ValidarAsync(request, id);
        if (erro is not null)
            return erro;

        regra!.Id = id;
        regra.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(regra, etapaIds!);

        var salva = await _repository.GetByIdAsync(id);
        return new RegraAvaliacaoCommandResult(true, "Regra de avaliação atualizada com sucesso!", MapToResponse(salva!));
    }

    public async Task<RegraAvaliacaoCommandResult> DeleteAsync(int id)
    {
        if (await _repository.GetByIdAsync(id) is null)
            return NotFound();

        await _repository.DeleteAsync(id);
        return new RegraAvaliacaoCommandResult(true, "Regra de avaliação excluída com sucesso! As etapas vinculadas passam a usar a regra padrão do sistema.");
    }

    private async Task<(RegraAvaliacao? Regra, IReadOnlyCollection<int>? EtapaIds, RegraAvaliacaoCommandResult? Erro)> ValidarAsync(
        RegraAvaliacaoRequest request, int? excludeId)
    {
        var nome = (request.Nome ?? string.Empty).Trim();
        if (nome.Length == 0)
            return Invalid("Informe o nome da regra de avaliação.");
        if (nome.Length > MaxNome)
            return Invalid($"O nome da regra deve ter no máximo {MaxNome} caracteres.");

        if (request.NotaMaxima <= 0 || request.NotaMaxima > RegraAvaliacao.NotaMaximaLimite)
            return Invalid($"A nota máxima deve ser maior que zero e no máximo {RegraAvaliacao.NotaMaximaLimite:0}.");

        if (request.MediaAprovacao <= 0 || request.MediaAprovacao > request.NotaMaxima)
            return Invalid("A média para aprovação deve ser maior que zero e não pode passar da nota máxima.");

        if (decimal.Round(request.NotaMaxima, 2) != request.NotaMaxima || decimal.Round(request.MediaAprovacao, 2) != request.MediaAprovacao)
            return Invalid("A nota máxima e a média aceitam no máximo duas casas decimais.");

        if (request.CasasDecimais < 0 || request.CasasDecimais > RegraAvaliacao.CasasDecimaisMaximo)
            return Invalid($"As casas decimais devem estar entre 0 e {RegraAvaliacao.CasasDecimaisMaximo}.");

        var calculo = (request.CalculoNotaPeriodo ?? string.Empty).Trim().ToUpperInvariant();
        if (!RegraAvaliacao.Calculos.Contains(calculo))
            return Invalid("Forma de cálculo inválida. Use média ponderada ou soma de pontos.");

        var substituicao = string.IsNullOrWhiteSpace(request.SubstituicaoRecuperacao)
            ? RegraAvaliacao.SubstituiMedia
            : request.SubstituicaoRecuperacao.Trim().ToUpperInvariant();
        if (!RegraAvaliacao.Substituicoes.Contains(substituicao))
            return Invalid("Regra de substituição da recuperação inválida. Use substituir a média, média com a recuperação ou recuperação limitada à média de aprovação.");

        var etapaIds = (request.EtapaEnsinoIds ?? new List<int>()).Distinct().ToList();
        if (etapaIds.Count > 0)
        {
            var existentes = (await _etapaRepository.GetAllAsync()).Select(e => e.Id).ToHashSet();
            if (etapaIds.Any(id => !existentes.Contains(id)))
            {
                return (null, null, new RegraAvaliacaoCommandResult(false, "Etapa de ensino não encontrada.",
                    Error: RegraAvaliacaoResultError.NotFound));
            }
        }

        if (await _repository.ExistsByNomeAsync(nome, excludeId))
        {
            return (null, null, new RegraAvaliacaoCommandResult(false, "Já existe uma regra de avaliação com este nome.",
                Error: RegraAvaliacaoResultError.Conflict));
        }

        var regra = new RegraAvaliacao
        {
            Nome = nome,
            NotaMaxima = request.NotaMaxima,
            MediaAprovacao = request.MediaAprovacao,
            CasasDecimais = request.CasasDecimais,
            CalculoNotaPeriodo = calculo,
            PermiteRecuperacao = request.PermiteRecuperacao,
            SubstituicaoRecuperacao = substituicao,
        };

        return (regra, etapaIds, null);
    }

    private static (RegraAvaliacao?, IReadOnlyCollection<int>?, RegraAvaliacaoCommandResult?) Invalid(string message)
        => (null, null, new RegraAvaliacaoCommandResult(false, message, Error: RegraAvaliacaoResultError.Validation));

    private static RegraAvaliacaoCommandResult NotFound()
        => new(false, "Regra de avaliação não encontrada.", Error: RegraAvaliacaoResultError.NotFound);

    private static RegraAvaliacaoResponse MapToResponse(RegraAvaliacao r)
        => new(
            r.Id == 0 ? null : r.Id,
            r.Nome,
            r.NotaMaxima,
            r.MediaAprovacao,
            r.CasasDecimais,
            r.CalculoNotaPeriodo,
            r.PermiteRecuperacao,
            r.SubstituicaoRecuperacao,
            r.Etapas
                .OrderBy(e => e.ModalidadeEnsino?.Nome)
                .ThenBy(e => e.OrdemCronologica)
                .Select(e => new RegraEtapaResponse(e.Id, e.Nome, e.ModalidadeEnsino?.Nome ?? string.Empty))
                .ToList());
}
