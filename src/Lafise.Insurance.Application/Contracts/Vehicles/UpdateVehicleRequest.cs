using System.ComponentModel.DataAnnotations;

namespace Lafise.Insurance.Application.Contracts.Vehicles;

/// <summary>Datos modificables de un vehículo existente.</summary>
public class UpdateVehicleRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "La placa es obligatoria.")]
    [StringLength(10, MinimumLength = 4, ErrorMessage = "La placa debe tener entre 4 y 10 caracteres.")]
    public string Plate { get; set; } = string.Empty;

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [StringLength(60)]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(60)]
    public string Model { get; set; } = string.Empty;

    [Range(1900, 2200, ErrorMessage = "El año del vehículo no es válido.")]
    public int Year { get; set; }

    [Range(0.01, 99_999_999.99, ErrorMessage = "El valor comercial debe ser mayor que cero.")]
    public decimal CommercialValue { get; set; }

    /// <summary>Permite reactivar un vehículo dado de baja.</summary>
    public bool IsActive { get; set; } = true;
}
