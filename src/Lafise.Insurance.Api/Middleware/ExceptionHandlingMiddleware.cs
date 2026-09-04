using System.Net.Mime;
using Lafise.Insurance.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Lafise.Insurance.Api.Middleware;

/// <summary>
/// Traduce las excepciones del dominio a respuestas HTTP semánticas usando ProblemDetails
/// (RFC 7807). Centralizarlo aquí mantiene los controladores libres de bloques try/catch.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var problem = BuildProblemDetails(exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Error no controlado al procesar {Method} {Path}.",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Solicitud rechazada en {Method} {Path}: {Detail}",
                context.Request.Method, context.Request.Path, problem.Detail);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        await context.Response.WriteAsJsonAsync(problem);
    }

    private ProblemDetails BuildProblemDetails(Exception exception) => exception switch
    {
        EntityNotFoundException notFound => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Recurso no encontrado",
            Detail = notFound.Message
        },

        ConflictException conflict => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflicto con el estado actual",
            Detail = conflict.Message,
            Extensions = { ["code"] = conflict.Code }
        },

        BusinessRuleViolationException businessRule => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Regla de negocio incumplida",
            Detail = businessRule.Message,
            Extensions = { ["code"] = businessRule.Code }
        },

        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Error interno del servidor",
            Detail = _environment.IsDevelopment()
                ? exception.ToString()
                : "Ocurrió un error inesperado. Contacte al administrador del sistema."
        }
    };
}

/// <summary>Extensión para registrar el middleware en el pipeline.</summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
