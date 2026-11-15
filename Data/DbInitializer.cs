using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoBlazor.Models;

namespace ProyectoBlazor.Data;

public static class DbInitializer
{
    public const string DefaultPassword = "StudyFlow_1234!";
    public const string AdminEmail = "admin@studyflow.com";
    public const string AdminPassword = "Admin_1234!";
    public const string AdminRole = "Admin";

    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var factory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        await using var context = await factory.CreateDbContextAsync();

        // Si faltan migraciones, no intentamos inicializar nada.
        if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            return;
        }

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<Usuario>>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<int>>>();

        // 1. Rol Admin
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            var rol = await roleManager.CreateAsync(
                new IdentityRole<int>(AdminRole));

            if (!rol.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo crear el rol Admin: " +
                    string.Join(", ", rol.Errors.Select(e => e.Description)));
            }
        }

        // 2. Completar datos de Identity y resetear contraseñas viejas
        foreach (var usuario in await userManager.Users.ToListAsync())
        {
            if (string.IsNullOrWhiteSpace(usuario.UserName))
            {
                usuario.UserName = usuario.Email;
            }

            usuario.EmailConfirmed = true;

            var actualizacion = await userManager.UpdateAsync(usuario);

            if (!actualizacion.Succeeded)
            {
                throw new InvalidOperationException(
                    $"No se pudo actualizar a {usuario.Email}: " +
                    string.Join(", ", actualizacion.Errors.Select(e => e.Description)));
            }

            if (RequiereReset(usuario.PasswordHash))
            {
                await userManager.RemovePasswordAsync(usuario);

                var reset = await userManager.AddPasswordAsync(
                    usuario, DefaultPassword);

                if (!reset.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"No se pudo resetear la contraseña de {usuario.Email}: " +
                        string.Join(", ", reset.Errors.Select(e => e.Description)));
                }
            }
        }

        // 3. Administrador
        var admin = await userManager.FindByEmailAsync(AdminEmail);

        if (admin is null)
        {
            admin = new Usuario
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                Nombre = "Administrador",
                FechaNacimiento = new DateOnly(1990, 1, 1),
                FechaRegistro = DateTime.Now,
                EmailConfirmed = true
            };

            var creacion = await userManager.CreateAsync(
                admin, AdminPassword);

            if (!creacion.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo crear el administrador: " +
                    string.Join(", ", creacion.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            var asignacion = await userManager.AddToRoleAsync(
                admin, AdminRole);

            if (!asignacion.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo asignar el rol Admin: " +
                    string.Join(", ", asignacion.Errors.Select(e => e.Description)));
            }
        }
    }

    private static bool RequiereReset(string? passwordHash)
        => string.IsNullOrEmpty(passwordHash)
        || passwordHash.StartsWith("$2");
}