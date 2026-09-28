namespace ActivosTi.Api.Contracts;

public sealed record CreateSupplierResponse(
    long Id,
    string Name,
    string? ContactEmail,
    bool Purchase,
    bool Maintenance,
    bool Rental
);