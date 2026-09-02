using Microsoft.EntityFrameworkCore;
using ProyectoBlazor.Data;
using ProyectoBlazor.Models;

namespace ProyectoBlazor.Services;

public class UsuarioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public UsuarioService(
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Usuario?> RegistrarUsuarioAsync(
    string nombre,
    string email,
    string password,
    DateOnly fechaNacimiento)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        bool existe = await context.Usuarios
            .AnyAsync(u => u.Email == email);

        if (existe)
        {
            return null;
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FechaNacimiento = fechaNacimiento,
            FechaRegistro = DateTime.Now
        };

        context.Usuarios.Add(usuario);

        try
        {
            await context.SaveChangesAsync();

            return usuario;
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException?.Message.Contains("IX_Usuarios_Email") == true)
            {
                return null;
            }

            // Si fue otro error de base de datos,
            // no lo ocultamos.
            throw;
        }
    }
    public async Task<List<Usuario>> ObtenerUsuariosAsync()
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await context.Usuarios
            .OrderBy(u => u.IdUsuario)
            .ToListAsync();
    }
}
