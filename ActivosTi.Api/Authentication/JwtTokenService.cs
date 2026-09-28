using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ActivosTi.Api.Authentication;

public sealed class JwtTokenService
{
    private readonly Jwt _jwt;

    public JwtTokenService(IOptions<Jwt> options)
    {
        _jwt = options.Value;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken( AuthenticatedUser user)
    {
        var expiresAtUtc =
            DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.RoleName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));

        var credentials = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);
        //Se firma la clave secreta y el algoritmo HMAC SHA256
        //Si un usuario malintencionado intenta modificar el token, la firma no coincidirá y el token será inválido.

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow, //valida el token a partir de la fecha actual
            expires: expiresAtUtc,
            signingCredentials: credentials
        );  //Se crea el token con los datos del usuario, la fecha de expiración y la firma

        return (new JwtSecurityTokenHandler().WriteToken(token),expiresAtUtc
        );
    }
}