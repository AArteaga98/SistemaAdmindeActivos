namespace ActivosTi.Api.Authentication;
    public sealed class Jwt
    {
      public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty; //Quien emite el token, init hace que no se pueda modificar despues de la inicializacion
    public string Audience { get; init; } = string.Empty; //Quien recibe el token
    public string SecretKey { get; init; } = string.Empty; //Clave secreta para firmar el token
    public int ExpirationMinutes { get; init; } = 60; //Tiempo de expiración del token en minutos
}

