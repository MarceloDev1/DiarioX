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

    public async Task<UsuarioPerfil?> GetGlobalByUsuarioIdAsync(int usuarioId)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(up => up.UsuarioId == usuarioId && up.EscolaId == null);
    }
}
