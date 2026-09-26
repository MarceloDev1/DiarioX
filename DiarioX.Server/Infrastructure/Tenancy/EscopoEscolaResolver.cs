using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Tenancy;

/// <summary>
/// Define as escolas que o usuário da requisição pode acessar dentro da instituição:
/// - Perfil vinculado a escolas (usuarios_perfis.escola_id preenchido): somente essas escolas.
/// - Perfil Professor sem escolas definidas: as escolas em que o professor leciona.
/// - Demais casos (perfil da rede toda, sem perfil): todas as escolas (nulo).
/// Deve ser chamado depois que a instituição da requisição foi definida no TenantContext.
/// </summary>
public class EscopoEscolaResolver
{
    private readonly AppDbContext _context;

    public EscopoEscolaResolver(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<int>?> ResolverAsync(int usuarioId)
    {
        var perfis = await _context.UsuariosPerfis
            .AsNoTracking()
            .Where(up => up.UsuarioId == usuarioId)
            .Select(up => new { up.EscolaId, PerfilNome = up.Perfil.Nome })
            .ToListAsync();

        var escolasDoPerfil = perfis.Where(p => p.EscolaId is not null).Select(p => p.EscolaId!.Value).Distinct().ToList();
        if (escolasDoPerfil.Count > 0)
            return escolasDoPerfil;

        var somenteProfessor = perfis.Count > 0 &&
            perfis.All(p => string.Equals(p.PerfilNome, Perfil.Professor, StringComparison.OrdinalIgnoreCase));
        if (!somenteProfessor)
            return null;

        return await _context.ProfessorEscolas
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .AsNoTracking()
            .Where(pe => pe.Professor.UsuarioId == usuarioId)
            .Select(pe => pe.EscolaId)
            .Distinct()
            .ToListAsync();
    }
}
