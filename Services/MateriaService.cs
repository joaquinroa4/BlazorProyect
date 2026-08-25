using Microsoft.EntityFrameworkCore;
using ProyectoBlazor.Data;
using ProyectoBlazor.Models;

namespace ProyectoBlazor.Services;

public class MateriaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public MateriaService(
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    //Traigo materias del usuaroo en orden alfabético
    public async Task<List<Materia>> ObtenerMateriasAsync(int usuarioId)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await context.Materias
            .Where(m => m.UsuarioId == usuarioId)
            .OrderBy(m => m.Nombre)
            .ToListAsync();
    }
//Creo una materia, la asigno al usuario y la guardo en SQL Server
    public async Task<bool> CrearMateriaAsync(
        string nombre,
        string? descripcion,
        int usuarioId)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var materia = new Materia
        {
            Nombre = nombre,
            Descripcion = descripcion,
            UsuarioId = usuarioId
        };

        context.Materias.Add(materia);

        await context.SaveChangesAsync();

        return true;
    }

//Editar una materia existente, verificando que pertenezca al usuario y actualizando sus propiedades en SQL Server
    public async Task<bool> ActualizarMateriaAsync(
    int idMateria,
    string nombre,
    string? descripcion,
    int usuarioId)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var materia = await context.Materias
            .FirstOrDefaultAsync(m => //Busco que la materia le pertenezca al usuario
                m.IdMateria == idMateria &&
                m.UsuarioId == usuarioId);

        if (materia == null)
        {
            return false;
        }

        materia.Nombre = nombre;
        materia.Descripcion = descripcion;

        await context.SaveChangesAsync();

        return true;
    }
    //Eliminar una materia, verificando que pertenezca al usuario
    // Elimina una materia verificando que pertenezca al usuario
    public async Task<bool> EliminarMateriaAsync(
        int idMateria,
        int usuarioId)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var materia = await context.Materias
            .FirstOrDefaultAsync(m =>
                m.IdMateria == idMateria &&
                m.UsuarioId == usuarioId);

        if (materia == null)
        {
            return false;
        }

        context.Materias.Remove(materia);

        await context.SaveChangesAsync();

        return true;
    }
}