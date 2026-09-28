namespace ActivosTi.Api.Contracts;

public sealed record CreateEmployeeResponse(
    long Id,
    string EmployeeNumber,
    string FullName,
    string Email,
    bool IsActive
);