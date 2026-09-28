namespace ActivosTi.Api.Authentication;

public sealed record AuthenticatedUser(
    long Id, 
    string UserName,
    string PasswordHash,
    string RoleName,
    bool IsActive
);