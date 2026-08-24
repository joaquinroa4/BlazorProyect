namespace ProyectoBlazor.Models;

public class Etiqueta
{
    public int IdEtiqueta { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public ICollection<TareaEtiqueta> TareasEtiquetas { get; set; } = new List<TareaEtiqueta>();
}