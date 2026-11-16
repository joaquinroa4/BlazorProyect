using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoBlazor.Data;
using ProyectoBlazor.Models;

namespace ProyectoBlazor.Services;

public class UsuarioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly UserManager<Usuario> _userManager;

    public UsuarioService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        UserManager<Usuario> userManager)
    {
        _contextFactory = contextFactory;
        _userManager = userManager;
    }

    // ==========================================================
    // REGISTRAR
    // ==========================================================

    public async Task<Usuario?> RegistrarUsuarioAsync(
    string nombre,
    string email,
    string password,
    DateOnly fechaNacimiento)
    {
        email = email.Trim();

        // Identity exige email único: si ya existe, no creamos nada.
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return null;
        }

        var usuario = new Usuario
        {
            UserName = email,
            Email = email,
            Nombre = nombre.Trim(),
            FechaNacimiento = fechaNacimiento,
            FechaRegistro = DateTime.Now,
            EmailConfirmed = true
        };

        // CreateAsync hashea la contraseña con Identity y completa
        // NormalizedEmail, NormalizedUserName y SecurityStamp.
        var creacion = await _userManager.CreateAsync(usuario, password);

        if (!creacion.Succeeded)
        {
            // Carrera con otro registro del mismo email.
            if (creacion.Errors.Any(e => e.Code == "DuplicateEmail"))
            {
                return null;
            }

            // Errores de política de contraseña, etc.
            throw new InvalidOperationException(
                string.Join(" ", creacion.Errors.Select(e => e.Description)));
        }

        return usuario;
    }

    // ==========================================================
    // LISTAR
    // ==========================================================

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

    // ==========================================================
    // ACTUALIZAR
    // ==========================================================

    public async Task<bool> ActualizarUsuarioAsync(
    int idUsuario,
    string nombre,
    string email,
    DateOnly fechaNacimiento)
    {
        var usuario = await _userManager.FindByIdAsync(
            idUsuario.ToString());

        if (usuario == null)
        {
            return false;
        }

        // Normalizar los datos
        nombre = nombre.Trim();
        email = email.Trim();

        // Comprobar que el email no pertenezca a otro usuario
        var otroUsuario = await _userManager.FindByEmailAsync(email);

        if (otroUsuario != null && otroUsuario.Id != idUsuario)
        {
            return false;
        }

        // Actualizar los datos personales
        usuario.Nombre = nombre;
        usuario.FechaNacimiento = fechaNacimiento;

        // El email se cambia con UserManager para que también
        // se actualicen NormalizedEmail y UserName; si no, el
        // usuario deja de poder iniciar sesión.
        if (!string.Equals(
                usuario.Email,
                email,
                StringComparison.OrdinalIgnoreCase))
        {
            var cambioEmail = await _userManager.SetEmailAsync(
                usuario, email);

            if (!cambioEmail.Succeeded)
            {
                return false;
            }

            var cambioUsuario = await _userManager.SetUserNameAsync(
                usuario, email);

            if (!cambioUsuario.Succeeded)
            {
                return false;
            }
        }

        var actualizacion = await _userManager.UpdateAsync(usuario);

        return actualizacion.Succeeded;
    }

    // ==========================================================
    // ELIMINAR
    // ==========================================================

    public async Task<bool> EliminarUsuarioAsync(int idUsuario)
    {
        var usuario = await _userManager.FindByIdAsync(
            idUsuario.ToString());

        if (usuario == null)
        {
            return false;
        }

        await using var context =
            await _contextFactory.CreateDbContextAsync();

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

        await context.SaveChangesAsync();

        // DeleteAsync limpia también roles, claims y logins.
        var eliminacion = await _userManager.DeleteAsync(usuario);

        return eliminacion.Succeeded;
    }
}
