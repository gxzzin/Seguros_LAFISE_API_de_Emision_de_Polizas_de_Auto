using System.ComponentModel.DataAnnotations;

namespace Lafise.Insurance.Application.Contracts.Coverages;

/// <summary>Datos requeridos para dar de alta una cobertura.</summary>
public class CreateCoverageRequest
{
    [Required(ErrorMessage = "El nombre de la cobertura es obligatorio.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 80 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    /// <summary>Tasa en porcentaje sobre la suma asegurada (por ejemplo 2.50 para 2.5%).</summary>
    [Range(0.01, 100, ErrorMessage = "La tasa debe estar entre 0.01 y 100.")]
    public decimal Rate { get; set; }

    public bool IsActive { get; set; } = true;
}
