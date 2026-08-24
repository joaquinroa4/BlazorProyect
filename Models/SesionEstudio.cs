
namespace ProyectoBlazor.Models;

public class SesionEstudio
{
    public int IdSesion { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public int DuracionMinutos { get; set; }

    public int TareaId { get; set; }

    public Tarea Tarea { get; set; } = null!;

    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;
}