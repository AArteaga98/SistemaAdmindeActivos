using System.ComponentModel.DataAnnotations;

namespace ActivosTi.Api.Contracts;

public sealed class CreateAssetRequest : IValidatableObject
{
    [Required]
    [StringLength(50)]
    public string AssetCode { get; init; } = string.Empty;

    [StringLength(100)]
    public string? SerialNumber { get; init; }

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

    [Required]
    [StringLength(160)]
    public string CurrentLocation { get; init; } = string.Empty;

    public DateOnly? PurchaseDate { get; init; }

    public DateOnly? RentalEndDate { get; init; }

    [StringLength(500)]
    public string? Observations { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(AssetCode))
        {
            yield return Error(
                "El código del activo es obligatorio.",
                nameof(AssetCode));
        }

        if (string.IsNullOrWhiteSpace(Category))
        {
            yield return Error(
                "La categoría es obligatoria.",
                nameof(Category));
        }

        if (string.IsNullOrWhiteSpace(Brand))
        {
            yield return Error(
                "La marca es obligatoria.",
                nameof(Brand));
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            yield return Error(
                "El modelo es obligatorio.",
                nameof(Model));
        }

        if (string.IsNullOrWhiteSpace(CurrentLocation))
        {
            yield return Error(
                "La ubicación es obligatoria.",
                nameof(CurrentLocation));
        }

        if (OwnershipType is not ("Propio" or "Arrendado"))
        {
            yield return Error(
                "El tipo de propiedad debe ser Propio o Arrendado.",
                nameof(OwnershipType));
        }

        if (OwnershipType == "Arrendado" &&
            SupplierId is null)
        {
            yield return Error(
                "Un activo arrendado debe tener proveedor.",
                nameof(SupplierId));
        }

        if (OwnershipType != "Arrendado" &&
            RentalEndDate is not null)
        {
            yield return Error(
                "Solo los activos arrendados pueden tener fecha de fin de arrendamiento.",
                nameof(RentalEndDate));
        }

        if (OwnershipType == "Arrendado" &&
     PurchaseDate is not null)
        {
            yield return Error(
                "Un activo arrendado no debe tener fecha de compra.",
                 nameof(PurchaseDate) );
        }

        if (OwnershipType == "Arrendado" &&
     RentalEndDate is null)
        {
            yield return Error(
                "Un activo arrendado debe tener fecha de fin de arrendamiento.",
                 nameof(PurchaseDate));
        }

    }

    private static ValidationResult Error(
        string message,
        string memberName)
    {
        return new ValidationResult(
            message,
            new[] { memberName });
    }
}