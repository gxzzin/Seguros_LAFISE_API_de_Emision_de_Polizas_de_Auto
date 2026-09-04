using System.ComponentModel.DataAnnotations;

namespace Lafise.Insurance.Application.Contracts.Coverages;

/// <summary>
/// Datos modificables de una cobertura. Cambiar la tasa no altera las pólizas ya emitidas:
/// cada una conserva la tasa con la que se calculó.
/// </summary>
public class UpdateCoverageRequest
{
    [Required(ErrorMessage = "El nombre de la cobertura es obligatorio.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 80 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    [Range(0.01, 100, ErrorMessage = "La tasa debe estar entre 0.01 y 100.")]
    public decimal Rate { get; set; }

    public bool IsActive { get; set; } = true;
}
