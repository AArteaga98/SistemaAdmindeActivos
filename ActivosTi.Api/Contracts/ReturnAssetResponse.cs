namespace ActivosTi.Api.Contracts;

public sealed record ReturnAssetResponse(
    long AssignmentId,
    long AssetId,
    string Status,
    string ReturnCondition
);