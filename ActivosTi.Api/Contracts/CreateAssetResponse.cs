namespace ActivosTi.Api.Contracts;

public sealed record CreateAssetResponse(
    long Id,
    string AssetCode,
    string Status
);