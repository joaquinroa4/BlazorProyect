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
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==========================================
        // CLAVES PRIMARIAS
        // ==========================================

        modelBuilder.Entity<Usuario>()
            .HasKey(u => u.IdUsuario);

        modelBuilder.Entity<Materia>()
            .HasKey(m => m.IdMateria);

        modelBuilder.Entity<Tarea>()
            .HasKey(t => t.IdTarea);

        modelBuilder.Entity<SesionEstudio>()
            .HasKey(s => s.IdSesion);

        modelBuilder.Entity<Etiqueta>()
            .HasKey(e => e.IdEtiqueta);

        // Clave primaria compuesta
        modelBuilder.Entity<TareaEtiqueta>()
            .HasKey(te => new
            {
                te.TareaId,
                te.EtiquetaId
            });

        modelBuilder.Entity<Usuario>()
        .HasIndex(u => u.Email)
        .IsUnique();
        // ==========================================
        // USUARIO -> MATERIA
        // ==========================================

        modelBuilder.Entity<Materia>()
            .HasOne(m => m.Usuario)
            .WithMany(u => u.Materias)
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);


        // ==========================================
        // USUARIO -> TAREA
        // ==========================================

        modelBuilder.Entity<Tarea>()
            .HasOne(t => t.Usuario)
            .WithMany(u => u.Tareas)
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade); // .OnDelete(DeleteBehavior.Cascade)<-- Borra los registros en cascada. Si se borra el registro padre, los registros hijos también se borran


        // ==========================================
        // MATERIA -> TAREA
        // ==========================================

        modelBuilder.Entity<Tarea>()
            .HasOne(t => t.Materia)
            .WithMany(m => m.Tareas)
            .HasForeignKey(t => t.MateriaId)
            .OnDelete(DeleteBehavior.Restrict); // .OnDelete(DeleteBehavior.Restrict)<-- No permite borrar el registro padre si tiene registros hijos   


        // ==========================================
        // USUARIO -> SESION DE ESTUDIO
        // ==========================================

        modelBuilder.Entity<SesionEstudio>()
            .HasOne(s => s.Usuario)
            .WithMany(u => u.SesionesEstudio)
            .HasForeignKey(s => s.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);


        // ==========================================
        // TAREA -> SESION DE ESTUDIO
        // ==========================================

        modelBuilder.Entity<SesionEstudio>()
            .HasOne(s => s.Tarea)
            .WithMany(t => t.SesionesEstudio)
            .HasForeignKey(s => s.TareaId)
            .OnDelete(DeleteBehavior.Cascade);


        // ==========================================
        // TAREA <-> ETIQUETA
        // RELACION MUCHOS A MUCHOS
        // ==========================================


        modelBuilder.Entity<TareaEtiqueta>()
            .HasOne(te => te.Tarea)
            .WithMany(t => t.TareasEtiquetas)
            .HasForeignKey(te => te.TareaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TareaEtiqueta>()
            .HasOne(te => te.Etiqueta)
            .WithMany(e => e.TareasEtiquetas)
            .HasForeignKey(te => te.EtiquetaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

//El EF traduce esto como las tablas de mi base de datos