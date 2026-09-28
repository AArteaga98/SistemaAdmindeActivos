namespace ActivosTi.Api.Contracts;

public sealed record AssetMovementDto(
    long Id,
    long AssetId,
    long? AssignmentId,
    string MovementType,
    string? PreviousStatus,
    string? NewStatus,
    string? PreviousLocation,
    string? NewLocation,
    long PerformedByUserId,
    string UserName,
    DateTime OccurredAt,
    string? Observations,
    long? EmployeeId,
    string? ReturnCondition
);