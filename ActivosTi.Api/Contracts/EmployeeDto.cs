namespace ActivosTi.Api.Contracts;

public sealed record EmployeeDto(
    long Id,
    string EmployeeNumber,
    string FullName,
    string Email,
    bool IsActive,
    DateTime CreatedAt
);