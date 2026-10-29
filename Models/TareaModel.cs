using System.ComponentModel.DataAnnotations;

namespace ProyectoBlazor.Models;

public class TareaModel
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(
        100,
        ErrorMessage = "El título no puede superar los 100 caracteres.")]
    public string Titulo { get; set; } = string.Empty;


    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = string.Empty;


    [CustomValidation(
    typeof(TareaModel),
    nameof(ValidarFechaEntrega))]
    public DateTime? FechaEntrega { get; set; }


    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Debés seleccionar una materia.")]
    public int MateriaId { get; set; }


    public EstadoTarea Estado { get; set; } = EstadoTarea.Pendiente;


    public PrioridadTarea Prioridad { get; set; } = PrioridadTarea.Media;


    public DificultadTarea Dificultad { get; set; } = DificultadTarea.Media;


    [Range(
        1,
        1440,
        ErrorMessage = "El tiempo estimado debe estar entre 1 y 1440 minutos.")]
    public int? TiempoEstimadoMinutos { get; set; }
    
    //Función para validar la fecha de entrega
    public static ValidationResult? ValidarFechaEntrega(
    DateTime? fechaEntrega,
    ValidationContext context)
{
    // La fecha de entrega es opcional.
    if (!fechaEntrega.HasValue)
    {
        return ValidationResult.Success;
    }

    // No se permiten fechas anteriores a hoy.
    if (fechaEntrega.Value.Date < DateTime.Today)
    {
        return new ValidationResult(
            "La fecha de entrega no puede ser anterior a hoy.");
    }

    return ValidationResult.Success;
}
}

//Este modelo es el que utiliza el formulario para no ensuciar la base de datos