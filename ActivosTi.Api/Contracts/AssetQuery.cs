using System.ComponentModel.DataAnnotations;

namespace ActivosTi.Api.Contracts;

public sealed class AssetQuery : IValidatableObject
{
    [StringLength(100)]
    public string? Search { get; init; }

    [StringLength(20)]
    public string? Status { get; init; }

    [StringLength(60)]
    public string? Category { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (Status is not null &&
            Status is not (
                "Disponible" or
                "Asignado" or
                "Mantenimiento" or
                "Retirado"))
        {
            yield return new ValidationResult(
                "El estado no es válido.",
                new[] { nameof(Status) });
        }
    }
}