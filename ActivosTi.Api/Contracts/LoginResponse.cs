namespace ActivosTi.Api.Contracts;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string UserName,
    string RoleName
);