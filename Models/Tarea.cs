
namespace ProyectoBlazor.Models;

public class Tarea
{
    public int IdTarea { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public EstadoTarea Estado { get; set; }

    public PrioridadTarea Prioridad { get; set; }

    public DificultadTarea Dificultad { get; set; }

    public int? TiempoEstimadoMinutos { get; set; }

    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public int MateriaId { get; set; }

    public Materia Materia { get; set; } = null!;

    public ICollection<SesionEstudio> SesionesEstudio { get; set; }
        = new List<SesionEstudio>();
}