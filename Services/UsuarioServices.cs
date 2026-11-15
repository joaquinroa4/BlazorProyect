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
            .OrderBy(u => u.Id)
            .ToListAsync();
    }

    public async Task<Usuario?> ObtenerUsuarioPorIdAsync(int idUsuario)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == idUsuario);
    }

    public async Task<bool> ActualizarUsuarioAsync(
    int idUsuario,
    string nombre,
    string email,
    DateOnly fechaNacimiento)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        // Buscar el usuario
        var usuario = await context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == idUsuario);

        if (usuario == null)
        {
            return false;
        }

        // Normalizar los datos
        nombre = nombre.Trim();
        email = email.Trim();

        // Comprobar que el email no pertenezca a otro usuario
        bool emailExiste = await context.Usuarios
            .AnyAsync(u =>
                u.Email == email &&
                u.Id != idUsuario);

        if (emailExiste)
        {
            return false;
        }

        // Actualizar los datos personales
        usuario.Nombre = nombre;
        usuario.Email = email;
        usuario.FechaNacimiento = fechaNacimiento;

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarUsuarioAsync(int idUsuario)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var usuario = await context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == idUsuario);

        if (usuario == null)
        {
            return false;
        }

        // Usuario -> SesionEstudio está en Restrict:
        // hay que borrar las sesiones primero.
        var sesiones = await context.SesionesEstudio
            .Where(s => s.UsuarioId == idUsuario)
            .ToListAsync();

        context.SesionesEstudio.RemoveRange(sesiones);

        // Tarea -> Materia también está en Restrict:
        // se borran tareas antes que materias.
        var tareas = await context.Tareas
            .Where(t => t.UsuarioId == idUsuario)
            .ToListAsync();

        context.Tareas.RemoveRange(tareas);

        var materias = await context.Materias
            .Where(m => m.UsuarioId == idUsuario)
            .ToListAsync();

        context.Materias.RemoveRange(materias);

        context.Usuarios.Remove(usuario);

        await context.SaveChangesAsync();

        return true;
    }
}
