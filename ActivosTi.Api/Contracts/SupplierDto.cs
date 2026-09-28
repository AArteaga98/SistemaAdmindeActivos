namespace ActivosTi.Api.Contracts;


    public sealed record SupplierDto(
       long Id,
       string Name,
       string? ContactEmail,
       bool IsActive,
       DateTime CreatedAt,
       bool Purchase,
       bool Maintenance,
       bool Rental
       );

