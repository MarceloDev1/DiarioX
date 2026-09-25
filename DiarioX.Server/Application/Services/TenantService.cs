using System.Text.RegularExpressions;
using DiarioX.Server.Application.DTOs.Tenants;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class TenantService : ITenantService
{
    // O slug vira subdomínio, então segue as regras de um rótulo DNS.
    private static readonly Regex SlugPattern = new("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.Compiled);

    // Subdomínios usados pela própria plataforma (área do Administrador global, site, API).
    private static readonly HashSet<string> ReservedSlugs = ["admin", "www", "api"];

    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public TenantService(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<TenantResponse>> GetAllAsync()
    {
        var tenants = await _tenantRepository.GetAllAsync();
        return tenants.Select(MapToResponse);
    }

    public async Task<TenantResponse?> GetByIdAsync(int id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id);
        return tenant is null ? null : MapToResponse(tenant);
    }

    public async Task<CurrentTenantResponse?> GetCurrentAsync()
    {
        if (_tenantContext.TenantId is not int tenantId)
            return null;

        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        return tenant is null ? null : new CurrentTenantResponse(tenant.Nome, tenant.Slug);
    }

    public async Task<TenantCommandResult> CreateAsync(TenantRequest request)
    {
        var normalized = NormalizeRequest(request);
        var validation = await ValidateRequestAsync(normalized, null);
        if (!validation.Success)
            return validation;

        var created = await _tenantRepository.AddAsync(new Tenant
        {
            Nome = normalized.Nome,
            Slug = normalized.Slug,
            Status = normalized.Status,
        });

        return new TenantCommandResult(true, "Instituição cadastrada com sucesso.", MapToResponse(created));
    }

    public async Task<TenantCommandResult> UpdateAsync(int id, TenantRequest request)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant is null)
            return new TenantCommandResult(false, "Instituição não encontrada.", Error: TenantResultError.NotFound);

        var normalized = NormalizeRequest(request);
        var validation = await ValidateRequestAsync(normalized, id);
        if (!validation.Success)
            return validation;

        tenant.Nome = normalized.Nome;
        tenant.Slug = normalized.Slug;
        tenant.Status = normalized.Status;

        await _tenantRepository.UpdateAsync(tenant);
        return new TenantCommandResult(true, "Instituição atualizada com sucesso.", MapToResponse(tenant));
    }

    private async Task<TenantCommandResult> ValidateRequestAsync(TenantRequest request, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return Invalid("Nome da instituição obrigatório.");

        if (request.Nome.Length > 255)
            return Invalid("Nome da instituição deve ter no máximo 255 caracteres.");

        if (!SlugPattern.IsMatch(request.Slug))
            return Invalid("Subdomínio inválido. Use apenas letras minúsculas, números e hífen (sem hífen no início ou no fim).");

        if (ReservedSlugs.Contains(request.Slug))
            return Invalid("Este subdomínio é reservado pelo sistema.");

        if (request.Status != Tenant.StatusAtivo && request.Status != Tenant.StatusInativo)
            return Invalid("Status inválido. Valores permitidos: ATIVO ou INATIVO.");

        if (await _tenantRepository.ExistsBySlugAsync(request.Slug, excludeId))
            return new TenantCommandResult(false, "Já existe uma instituição com este subdomínio.", Error: TenantResultError.Conflict);

        return new TenantCommandResult(true, string.Empty);
    }

    private static TenantRequest NormalizeRequest(TenantRequest request) => new()
    {
        Nome = (request.Nome ?? string.Empty).Trim(),
        Slug = (request.Slug ?? string.Empty).Trim().ToLowerInvariant(),
        Status = (request.Status ?? string.Empty).Trim().ToUpperInvariant(),
    };

    private static TenantCommandResult Invalid(string message) =>
        new(false, message, Error: TenantResultError.Validation);

    private static TenantResponse MapToResponse(Tenant tenant) => new(
        tenant.Id,
        tenant.Nome,
        tenant.Slug,
        tenant.Status,
        tenant.CreatedAt);
}
