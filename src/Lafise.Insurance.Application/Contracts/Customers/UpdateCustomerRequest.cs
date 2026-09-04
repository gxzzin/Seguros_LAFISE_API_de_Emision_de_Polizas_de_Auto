using System.ComponentModel.DataAnnotations;

namespace Lafise.Insurance.Application.Contracts.Customers;

/// <summary>Datos modificables de un cliente existente.</summary>
public class UpdateCustomerRequest
{
    [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La identificación es obligatoria.")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "La identificación debe tener entre 5 y 20 caracteres.")]
    public string IdentificationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Permite reactivar un cliente dado de baja.</summary>
    public bool IsActive { get; set; } = true;
}
