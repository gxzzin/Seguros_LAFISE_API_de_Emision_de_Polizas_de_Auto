using System.ComponentModel.DataAnnotations;

namespace Lafise.Insurance.Application.Contracts.Policies;

/// <summary>
/// Solicitud de emisión: cliente existente, datos del vehículo y coberturas seleccionadas.
/// </summary>
public class IssuePolicyRequest
{
    /// <summary>Identificador del cliente que ya existe en el catálogo.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un cliente válido.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Los datos del vehículo son obligatorios.")]
    public VehicleRequest Vehicle { get; set; } = new();

    /// <summary>Identificadores de las coberturas a aplicar. Debe contener al menos una.</summary>
    [Required(ErrorMessage = "Debe seleccionar al menos una cobertura.")]
    [MinLength(1, ErrorMessage = "Debe seleccionar al menos una cobertura.")]
    public List<int> CoverageIds { get; set; } = new();
}
