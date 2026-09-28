using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using ActivosTi.Api.Authentication;
using ActivosTi.Api.Data;
using ActivosTi.Api.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuración de JWT
builder.Services.Configure<Jwt>(builder.Configuration.GetSection(Jwt.SectionName));

var jwtOptions = builder.Configuration
    .GetSection(Jwt.SectionName)
    .Get<Jwt>()
    ?? throw new InvalidOperationException(
        "Falta la configuración JWT.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
{
    throw new InvalidOperationException(
        "Falta el emisor del JWT.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException(
        "Falta la audiencia del JWT.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) ||
    jwtOptions.SecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "La clave JWT debe tener al menos 32 caracteres.");
}

if (jwtOptions.ExpirationMinutes <= 0)
{
    throw new InvalidOperationException(
        "El tiempo de expiración del JWT debe ser mayor que cero.");
}

// Limitación de intentos de inicio de sesión
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy("LoginPolicy", httpContext =>
    {
        var remoteIp =
            httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        //Cada IP tiene un límite de 3 intentos de inicio de sesión por minuto.
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: remoteIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.OnRejected = async (
        context,
        cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;

        context.HttpContext.Response.ContentType =
            "application/problem+json";

        context.HttpContext.Response.Headers.RetryAfter = "60";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                type = "https://httpstatuses.com/429",
                title = "Demasiados intentos",
                status = StatusCodes.Status429TooManyRequests,
                detail =
                    "Espera un minuto antes de volver a intentarlo."
            },
            cancellationToken);
    };
});


builder.Services.AddControllers();

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); //Aqui registramos nuestro manejador de excepciones globales

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Escribe únicamente el JWT."
        });

    //Le indicamos a Swagger que los endpoints utilizan el esquema Bearer.
    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type =
                            ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});


// Acceso a datos

builder.Services.AddSingleton<SqlConnectionFactory>();

builder.Services.AddScoped<UserRepository>();

builder.Services.AddScoped<SupplierRepository>();
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<AssetRepository>();
builder.Services.AddScoped<AssetOperationsRepository>();

builder.Services.AddScoped<IPasswordHasher<AuthenticatedUser>,PasswordHasher<AuthenticatedUser>>();



builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddScoped<ActivosTi.Api.Authentication.AuthenticationService>();

builder.Services.AddScoped<DevelopmentUserSeeder>();

// Autenticación y autorización


//Registramos la autenticación
//Cuando[Authorize] necesita autenticar una solicitud, utilizará JWT Bearer.
builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.SecretKey)),

                ValidateLifetime = true,

                RequireExpirationTime = true,

                ClockSkew = TimeSpan.Zero, //El token expira exactamente en el tiempo especificado, sin margen de tolerancia.

                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
    }); 

builder.Services.AddAuthorization();


var app = builder.Build();


app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting(); 

app.UseRateLimiter();

app.UseAuthentication(); //Este middleware identifica al usuario
app.UseAuthorization();//Este middleware valida si el usuario tiene permisos para acceder al recurso solicitado

app.MapControllers();



if (app.Environment.IsDevelopment())
{
    await using var scope =
        app.Services.CreateAsyncScope();

    var seeder = scope.ServiceProvider
        .GetRequiredService<DevelopmentUserSeeder>();

    await seeder.SeedAsync();
}

app.Run();