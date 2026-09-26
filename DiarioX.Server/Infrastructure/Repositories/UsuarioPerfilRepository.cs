using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class UsuarioPerfilRepository : BaseRepository<UsuarioPerfil>, IUsuarioPerfilRepository
{
    public UsuarioPerfilRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<UsuarioPerfil>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(up => up.UsuarioId == usuarioId)
            .ToListAsync();
    }

    public async Task SubstituirAsync(int usuarioId, int perfilId, IReadOnlyCollection<int> escolaIds)
    {
        var atuais = await _dbSet
            .Where(up => up.UsuarioId == usuarioId)
            .ToListAsync();

        var desejadas = escolaIds.Count == 0
            ? new HashSet<int?> { null }
            : escolaIds.Select(id => (int?)id).ToHashSet();

        // Mantém as linhas que continuam valendo; remove e inclui só a diferença, sem esbarrar nos
        // índices únicos de usuário+perfil(+escola).
        var manter = atuais.Where(up => up.PerfilId == perfilId && desejadas.Contains(up.EscolaId)).ToList();
        _dbSet.RemoveRange(atuais.Except(manter));

        foreach (var escolaId in desejadas.Where(e => manter.All(up => up.EscolaId != e)))
            _dbSet.Add(new UsuarioPerfil { UsuarioId = usuarioId, PerfilId = perfilId, EscolaId = escolaId });

        await _context.SaveChangesAsync();
    }
}
