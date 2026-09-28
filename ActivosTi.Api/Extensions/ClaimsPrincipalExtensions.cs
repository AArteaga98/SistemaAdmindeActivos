using System.Security.Claims;

namespace ActivosTi.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long GetRequiredUserId(
        this ClaimsPrincipal user)
    {
        var value =
            user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException(
                "El token no contiene un identificador válido.");
        }

        return userId;
    }
}