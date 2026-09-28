namespace ActivosTi.Api.Contracts;

public sealed record AssignAssetResponse(
    long AssignmentId,
    long AssetId,
    long EmployeeId,
    string Status
);