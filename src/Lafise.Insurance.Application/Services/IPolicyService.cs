using Lafise.Insurance.Application.Contracts.Policies;

namespace Lafise.Insurance.Application.Services;

/// <summary>Casos de uso de emisión y consulta de pólizas.</summary>
public interface IPolicyService
{
    /// <summary>Emite una nueva póliza aplicando las reglas de suscripción y el cálculo de la prima.</summary>
    Task<PolicyDetailDto> IssueAsync(IssuePolicyRequest request, CancellationToken cancellationToken = default);

    /// <summary>Obtiene el detalle de una póliza emitida.</summary>
    Task<PolicyDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Historial de todas las emisiones realizadas.</summary>
    Task<IReadOnlyList<PolicySummaryDto>> GetHistoryAsync(CancellationToken cancellationToken = default);
}
