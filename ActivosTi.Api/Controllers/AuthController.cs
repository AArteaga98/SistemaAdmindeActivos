using ActivosTi.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ActivosTi.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ActivosTi.Api.Authentication.AuthenticationService
        _authenticationService;

    public AuthController(
        ActivosTi.Api.Authentication.AuthenticationService
            authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")] //Se configuró en Program.cs
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request,CancellationToken cancellationToken)
    {
        var response = await _authenticationService.LoginAsync(
            request,
            cancellationToken);

        if (response is null)
        {
            return Unauthorized(new
            {
                message = "Usuario o contraseña incorrectos."
            });
        }

        return Ok(response);
    }
}