using System.ComponentModel.DataAnnotations;

namespace ActivosTi.Api.Contracts;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [StringLength(100)]
    public string UserName { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(200, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}
