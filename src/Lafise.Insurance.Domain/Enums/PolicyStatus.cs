namespace Lafise.Insurance.Domain.Enums;

/// <summary>
/// Estados posibles del ciclo de vida de una póliza.
/// </summary>
public enum PolicyStatus
{
    /// <summary>Póliza vigente; bloquea la emisión de otra póliza para la misma placa y cliente.</summary>
    Active = 1,

    /// <summary>Póliza anulada antes de su vencimiento.</summary>
    Cancelled = 2,

    /// <summary>Póliza cuya vigencia ya expiró.</summary>
    Expired = 3
}
