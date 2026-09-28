using System.ComponentModel.DataAnnotations;

namespace ActivosTi.Api.Contracts;

public sealed class CreateSupplierRequest : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(
        160,
        ErrorMessage = "El nombre no puede exceder 160 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [EmailAddress(
        ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    [StringLength(
        254,
        ErrorMessage = "El correo no puede exceder 254 caracteres.")]
    public string? ContactEmail { get; init; }

    public bool Purchase { get; init; }

    public bool Maintenance { get; init; }

    public bool Rental { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!Purchase && !Maintenance && !Rental)
        {
            yield return new ValidationResult(
                "El proveedor debe ofrecer al menos un servicio.",
                new[]
                {
                    nameof(Purchase),
                    nameof(Maintenance),
                    nameof(Rental)
                });
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "El nombre no puede contener solamente espacios.",
                new[] { nameof(Name) });
        }
    }
}