using System.ComponentModel.DataAnnotations;

namespace ProyectoBlazor.Models;

public class RegistroModel
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El email no es válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]

    //CustomValidation --> Además de las validaciones normales, cuando valida FechaNacimiento, ejecuta también ValidarFechaNacimiento
    [CustomValidation(
        typeof(RegistroModel),
        nameof(ValidarFechaNacimiento))]
    public DateOnly FechaNacimiento { get; set; }


    public static ValidationResult? ValidarFechaNacimiento(
        DateOnly fechaNacimiento,
        ValidationContext context)
    {
        // Fecha no ingresada
        if (fechaNacimiento == default)
        {
            return new ValidationResult(
                "La fecha de nacimiento es obligatoria.");
        }

        // Fecha futura
        if (fechaNacimiento > DateOnly.FromDateTime(DateTime.Today))
        {
            return new ValidationResult(
                "La fecha de nacimiento no puede ser futura.");
        }
        // Edad máxima: 100 años
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var fechaMinima = hoy.AddYears(-100);
        if (fechaNacimiento < fechaMinima)
        {
            return new ValidationResult(
                "La edad máxima permitida es de 100 años.");
        }

        // Calcular edad de forma correcta
        

        int edad = hoy.Year - fechaNacimiento.Year;

        if (fechaNacimiento > hoy.AddYears(-edad))
        {
            edad--;
        }

        // Edad mínima
        if (edad < 16)
        {
            return new ValidationResult(
                "El usuario debe tener al menos 16 años.");
        }

        return ValidationResult.Success;
    }
}