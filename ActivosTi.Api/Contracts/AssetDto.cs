namespace ActivosTi.Api.Contracts;

public sealed record AssetDto(
    long Id,
    string AssetCode,
    string? SerialNumber,
    string Category,
    string Brand,
    string Model,
    string OwnershipType,
    long? SupplierId,
    string Status,
    string CurrentLocation,
    DateOnly? PurchaseDate,
    DateOnly? RentalEndDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    long? AssignedEmployeeId,
    string? AssignedEmployeeName
);