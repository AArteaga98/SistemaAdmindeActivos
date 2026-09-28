using ActivosTi.Api.Contracts;
using ActivosTi.Api.Data;
using Microsoft.AspNetCore.Identity;

namespace ActivosTi.Api.Authentication;

public sealed class AuthenticationService
{
    private readonly UserRepository _userRepository;
    private readonly IPasswordHasher<AuthenticatedUser> _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;

    public AuthenticationService(
        UserRepository userRepository,
        IPasswordHasher<AuthenticatedUser> passwordHasher,
        JwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindForLoginAsync(
            request.UserName.Trim(),
            cancellationToken);

        if (user is null || !user.IsActive) //valida si el usuario existe y si esta activo
        {
            return null;
        }

        // Verifica si la contraseña proporcionada coincide con la contraseña en la base de datos
        var verificationResult =_passwordHasher.VerifyHashedPassword( user, user.PasswordHash, request.Password);
      

        if (verificationResult == PasswordVerificationResult.Failed) //Valida que la contraseña sea correcta
        {
            return null;
        }

        var token = _jwtTokenService.CreateToken(user);

        return new LoginResponse(
            AccessToken: token.Token,
            ExpiresAtUtc: token.ExpiresAtUtc,
            UserName: user.UserName,
            RoleName: user.RoleName
        );
    }
}