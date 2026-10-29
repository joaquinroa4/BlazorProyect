using Microsoft.EntityFrameworkCore;
using ProyectoBlazor.Data;
using ProyectoBlazor.Models;

namespace ProyectoBlazor.Services;

public class TareaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public TareaService(
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Tarea>> ObtenerTareasAsync(int usuarioId)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await context.Tareas
            .Include(t => t.Materia)
            .Where(t => t.UsuarioId == usuarioId)
            .OrderBy(t => t.FechaEntrega)
            .ToListAsync();
    }

    public async Task<bool> CrearTareaAsync(
        string titulo,
        string descripcion,
        DateTime? fechaEntrega,
        EstadoTarea estado,
        PrioridadTarea prioridad,
        DificultadTarea dificultad,
        int? tiempoEstimadoMinutos,
        int materiaId,
        int usuarioId)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var materiaExiste = await context.Materias
            .AnyAsync(m =>
                m.IdMateria == materiaId &&
                m.UsuarioId == usuarioId);

        if (!materiaExiste)
            return false;

        var tarea = new Tarea
        {
            Titulo = titulo,
            Descripcion = descripcion,
            FechaCreacion = DateTime.Now,
            FechaEntrega = fechaEntrega,
            Estado = estado,
            Prioridad = prioridad,
            Dificultad = dificultad,
            TiempoEstimadoMinutos = tiempoEstimadoMinutos,
            MateriaId = materiaId,
            UsuarioId = usuarioId
        };

        context.Tareas.Add(tarea);

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ActualizarTareaAsync(
        int idTarea,
        string titulo,
        string descripcion,
        DateTime? fechaEntrega,
        EstadoTarea estado,
        PrioridadTarea prioridad,
        DificultadTarea dificultad,
        int? tiempoEstimadoMinutos,
        int materiaId,
        int usuarioId)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var tarea = await context.Tareas
            .FirstOrDefaultAsync(t =>
                t.IdTarea == idTarea &&
                t.UsuarioId == usuarioId);

        if (tarea == null)
            return false;

        var materiaExiste = await context.Materias
            .AnyAsync(m =>
                m.IdMateria == materiaId &&
                m.UsuarioId == usuarioId);

        if (!materiaExiste)
            return false;

        tarea.Titulo = titulo;
        tarea.Descripcion = descripcion;
        tarea.FechaEntrega = fechaEntrega;
        tarea.Estado = estado;
        tarea.Prioridad = prioridad;
        tarea.Dificultad = dificultad;
        tarea.TiempoEstimadoMinutos = tiempoEstimadoMinutos;
        tarea.MateriaId = materiaId;

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarTareaAsync(
        int idTarea,
        int usuarioId)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var tarea = await context.Tareas
            .FirstOrDefaultAsync(t =>
                t.IdTarea == idTarea &&
                t.UsuarioId == usuarioId);

        if (tarea == null)
            return false;

        context.Tareas.Remove(tarea);

        await context.SaveChangesAsync();

        return true;
    }
}