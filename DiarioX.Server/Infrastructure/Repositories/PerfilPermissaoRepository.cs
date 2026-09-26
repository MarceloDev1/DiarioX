using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class PerfilPermissaoRepository : IPerfilPermissaoRepository
{
    private readonly AppDbContext _context;

    public PerfilPermissaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PerfilPermissao>> GetAllAsync()
    {
        return await _context.PerfisPermissoes
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlySet<string>> GetPermissoesDoUsuarioAsync(int usuarioId)
    {
        // Os filtros globais restringem perfis do usuário e permissões à instituição atual.
        var perfilIds = _context.UsuariosPerfis
            .Where(up => up.UsuarioId == usuarioId)
            .Select(up => up.PerfilId);

        var permissoes = await _context.PerfisPermissoes
            .AsNoTracking()
            .Where(pp => perfilIds.Contains(pp.PerfilId))
            .Select(pp => pp.Permissao)
            .Distinct()
            .ToListAsync();

        return permissoes.ToHashSet(StringComparer.Ordinal);
    }

    public async Task ReplaceAsync(int perfilId, IEnumerable<string> permissoes)
    {
        var atuais = await _context.PerfisPermissoes
            .Where(pp => pp.PerfilId == perfilId)
            .ToListAsync();

        var desejadas = permissoes.ToHashSet(StringComparer.Ordinal);
        var atuaisPorCodigo = atuais.Select(pp => pp.Permissao).ToHashSet(StringComparer.Ordinal);

        _context.PerfisPermissoes.RemoveRange(atuais.Where(pp => !desejadas.Contains(pp.Permissao)));
        _context.PerfisPermissoes.AddRange(desejadas
            .Where(p => !atuaisPorCodigo.Contains(p))
            .Select(p => new PerfilPermissao { PerfilId = perfilId, Permissao = p }));

        await _context.SaveChangesAsync();
    }

    public async Task<bool> TenantPossuiPermissoesAsync(int tenantId)
    {
        return await _context.PerfisPermissoes
            .IgnoreQueryFilters()
            .AnyAsync(pp => pp.TenantId == tenantId);
    }

    public async Task AddRangeAsync(IEnumerable<PerfilPermissao> permissoes)
    {
        _context.PerfisPermissoes.AddRange(permissoes);
        await _context.SaveChangesAsync();
    }
}
