using System.ComponentModel.DataAnnotations;

namespace ActivosTi.Api.Contracts;

public sealed class UpdateAssetRequest : IValidatableObject
{
    [Required]
    [StringLength(60)]
    public string Category { get; init; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Brand { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Model { get; init; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string OwnershipType { get; init; } = string.Empty;

    public long? SupplierId { get; init; }

    public DateOnly? PurchaseDate { get; init; }

    public DateOnly? RentalEndDate { get; init; }

    [Required]
    [StringLength(160)]
    public string CurrentLocation { get; init; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Status { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Observations { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Category) ||
            string.IsNullOrWhiteSpace(Brand) ||
            string.IsNullOrWhiteSpace(Model) ||
            string.IsNullOrWhiteSpace(CurrentLocation))
        {
            yield return new ValidationResult(
                "Categoría, marca, modelo y ubicación son obligatorios.");
        }

        if (OwnershipType is not ("Propio" or "Arrendado"))
        {
            yield return new ValidationResult(
                "El tipo de propiedad debe ser Propio o Arrendado.",
                new[] { nameof(OwnershipType) });
        }

        if (OwnershipType == "Arrendado" &&
            SupplierId is null)
        {
            yield return new ValidationResult(
                "Un activo arrendado debe tener proveedor.",
                new[] { nameof(SupplierId) });
        }

        if (OwnershipType != "Arrendado" &&
            RentalEndDate is not null)
        {
            yield return new ValidationResult(
                "Solo un activo arrendado puede tener fecha de fin de arrendamiento.",
                new[] { nameof(RentalEndDate) });
        }

        if (Status is not (
            "Disponible" or
            "Asignado" or
            "Mantenimiento" or
            "Retirado"))
        {
            yield return new ValidationResult(
                "El estado no es válido.",
                new[] { nameof(Status) });
        }

        if (OwnershipType == "Arrendado" &&
    PurchaseDate is not null)
        {
            yield return new ValidationResult(
                "Un activo arrendado no debe tener fecha de compra.",
                new[] { nameof(PurchaseDate) });
        }
        if (OwnershipType == "Arrendado" &&
     RentalEndDate is null)
        {
            yield return new ValidationResult(
                "Un activo arrendado debe tener fecha de fin de arrendamiento.",
                 new[] { nameof(PurchaseDate) });
        }

    }
}