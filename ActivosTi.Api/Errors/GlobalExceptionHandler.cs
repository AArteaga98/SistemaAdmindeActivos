using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ActivosTi.Api.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Ocurrió un error procesando {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var problem = CreateProblemDetails(exception);

        httpContext.Response.StatusCode =
            problem.Status
            ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails( Exception exception)
    {
        if (exception is UnauthorizedAccessException)
        {
            return new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "No autorizado",
                Detail = "El token no contiene una identidad válida."
            };
        }

        if (exception is SqlException sqlException)
        {
            return sqlException.Number switch
            {
                2601 or 2627 => Problem(
                    StatusCodes.Status409Conflict,
                    "Registro duplicado",
                    "Ya existe un registro con los mismos datos únicos."),

                51020 => Problem(
                    StatusCodes.Status404NotFound,
                    "Activo no encontrado",
                    "El activo solicitado no existe."),

                51024 => Problem(
                    StatusCodes.Status400BadRequest,
                    "Datos inválidos",
                    sqlException.Message),

                >= 51000 and <= 51999 => Problem(
                    StatusCodes.Status409Conflict,
                    "Operación no permitida",
                    sqlException.Message),

                _ => InternalServerError()
            };
        }

        return InternalServerError();
    }

    private static ProblemDetails Problem(int status,string title, string detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };
    }

    private static ProblemDetails InternalServerError()
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Error interno",
            Detail = "Ocurrió un error inesperado. Intenta nuevamente."
        };
    }
}