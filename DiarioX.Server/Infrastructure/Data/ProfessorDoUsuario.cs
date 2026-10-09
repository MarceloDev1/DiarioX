using DiarioX.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Data;

public static class ProfessorDoUsuario
{
    /// <summary>
    /// Cadastro de professor do usuário: o vinculado a ele (usuario_id) ou, sem vínculo, o de mesmo e-mail ou CPF
    /// na instituição. O cadastro automático do usuário falha quando o e-mail já tinha conta, e o professor fica
    /// sem vínculo; sem esta busca o professor perderia o escopo das próprias turmas e a home do professor.
    /// Ignora o filtro de escola: o vínculo vale para a instituição inteira (e é usado para montar esse filtro).
    /// O vinculado vem primeiro.
    /// </summary>
    public static IQueryable<Professor> ProfessoresDoUsuario(this AppDbContext context, int usuarioId)
    {
        var usuario = context.Users.IgnoreQueryFilters().Where(u => u.Id == usuarioId);

        return context.Professores
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .Where(p => p.UsuarioId == usuarioId ||
                        (p.UsuarioId == null && usuario.Any(u =>
                            u.TenantId == p.TenantId &&
                            (u.Email.ToLower() == p.Email.ToLower() || u.Cpf == p.Cpf))))
            .OrderByDescending(p => p.UsuarioId == usuarioId);
    }
}
