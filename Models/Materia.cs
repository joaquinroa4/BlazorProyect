namespace ProyectoBlazor.Models;

public class Materia
{
    public int IdMateria { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Color { get; set; } = "#0d6efd";

    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}