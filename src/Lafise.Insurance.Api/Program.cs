using System.Reflection;
using System.Text.Json.Serialization;
using Lafise.Insurance.Api.Extensions;
using Lafise.Insurance.Api.Middleware;
using Lafise.Insurance.Application;
using Lafise.Insurance.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Seguros LAFISE - API de Emisión de Pólizas de Auto",
        Version = "v1",
        Description = "Prototipo del módulo de emisión: catálogos, cálculo de prima y consulta de pólizas."
    });

    // Publica los comentarios XML del proyecto en Swagger.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandling();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Emisión de Pólizas v1");
    options.DocumentTitle = "Seguros LAFISE - Emisión de Pólizas";
});

app.MapControllers();

// Redirige la raíz a Swagger para facilitar la revisión.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    await app.ApplyMigrationsAsync();
}

app.Run();

/// <summary>Expuesto como parcial para permitir pruebas de integración con WebApplicationFactory.</summary>
public partial class Program
{
}
