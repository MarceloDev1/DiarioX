using DiarioX.Server.Application.DTOs.Tenants;

namespace DiarioX.Server.Application.Interfaces;

public interface ITenantService
{
    Task<IEnumerable<TenantResponse>> GetAllAsync();
    Task<TenantResponse?> GetByIdAsync(int id);
    Task<CurrentTenantResponse?> GetCurrentAsync();
    Task<TenantCommandResult> CreateAsync(TenantRequest request);
    Task<TenantCommandResult> UpdateAsync(int id, TenantRequest request);
}
