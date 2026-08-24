using Microsoft.EntityFrameworkCore;
using ProyectoBlazor.Models;

namespace ProyectoBlazor.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Materia> Materias { get; set; }
    public DbSet<Tarea> Tareas { get; set; }
    public DbSet<SesionEstudio> SesionesEstudio { get; set; }
    public DbSet<Etiqueta> Etiquetas { get; set; }
    public DbSet<TareaEtiqueta> TareasEtiquetas { get; set; }
}

//El EF traduce esto como las tablas de mi base de datos