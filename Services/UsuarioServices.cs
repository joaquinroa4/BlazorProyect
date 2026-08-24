using Microsoft.EntityFrameworkCore;
using ProyectoBlazor.Data;
using ProyectoBlazor.Models;

namespace ProyectoBlazor.Services;

public class UsuarioService
{
    private readonly ApplicationDbContext _context;

    public UsuarioService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RegistrarUsuarioAsync(
        string nombre,
        string email,
        string password,
        DateOnly fechaNacimiento)
    {
        // Comprobar si el email ya existe
        bool existe = await _context.Usuarios
            .AnyAsync(u => u.Email == email);

        if (existe)
            return false;

        var usuario = new Usuario
        {
            Nombre = nombre,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FechaNacimiento = fechaNacimiento,
            FechaRegistro = DateTime.Now
        };

        _context.Usuarios.Add(usuario);

        await _context.SaveChangesAsync();

        return true;
    }
}