using System.ComponentModel.DataAnnotations;

namespace ActivosTi.Api.Contracts;

public sealed class CreateEmployeeRequest : IValidatableObject
{
    [Required(ErrorMessage = "El número de empleado es obligatorio.")]
    [StringLength(
        40,
        ErrorMessage = "El número de empleado no puede exceder 40 caracteres.")]
    public string EmployeeNumber { get; init; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(
        160,
        ErrorMessage = "El nombre no puede exceder 160 caracteres.")]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(
        ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    [StringLength(
        254,
        ErrorMessage = "El correo no puede exceder 254 caracteres.")]
    public string Email { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(EmployeeNumber))
        {
            yield return new ValidationResult(
                "El número de empleado no puede contener solamente espacios.",
                new[] { nameof(EmployeeNumber) });
        }

        if (string.IsNullOrWhiteSpace(FullName))
        {
            yield return new ValidationResult(
                "El nombre no puede contener solamente espacios.",
                new[] { nameof(FullName) });
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult(
                "El correo no puede contener solamente espacios.",
                new[] { nameof(Email) });
        }
    }
}