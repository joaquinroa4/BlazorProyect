using System.ComponentModel.DataAnnotations;

namespace ProyectoBlazor.Models;

public class Usuario
{
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El email no es válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
    public DateOnly FechaNacimiento { get; set; }

    public DateTime FechaRegistro { get; set; }

    public ICollection<Materia> Materias { get; set; } = new List<Materia>();

    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();

    public ICollection<SesionEstudio> SesionesEstudio { get; set; } = new List<SesionEstudio>();
}