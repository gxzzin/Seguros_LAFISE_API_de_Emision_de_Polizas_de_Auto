using Lafise.Insurance.Application.Abstractions;
using Lafise.Insurance.Application.Contracts.Coverages;
using Lafise.Insurance.Application.Mapping;
using Lafise.Insurance.Domain.Entities;
using Lafise.Insurance.Domain.Exceptions;

namespace Lafise.Insurance.Application.Services;

/// <summary>CRUD del catálogo de coberturas.</summary>
public interface ICoverageService
{
    Task<IReadOnlyList<CoverageDto>> GetAllAsync(bool onlyActive, CancellationToken cancellationToken = default);

    Task<CoverageDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CoverageDto> CreateAsync(CreateCoverageRequest request, CancellationToken cancellationToken = default);

    Task<CoverageDto> UpdateAsync(int id, UpdateCoverageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Descontinúa la cobertura (baja lógica). Las pólizas ya emitidas la conservan con la
    /// tasa que se les aplicó, por lo que no hace falta bloquear la operación.
    /// </summary>
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ICoverageService"/>
public class CoverageService : ICoverageService
{
    private readonly ICoverageRepository _coverageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CoverageService(ICoverageRepository coverageRepository, IUnitOfWork unitOfWork)
    {
        _coverageRepository = coverageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CoverageDto>> GetAllAsync(
        bool onlyActive,
        CancellationToken cancellationToken = default)
    {
        var coverages = await _coverageRepository.GetAllAsync(onlyActive, cancellationToken);
        return coverages.Select(coverage => coverage.ToDto()).ToList();
    }

    public async Task<CoverageDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var coverage = await FindOrThrowAsync(id, cancellationToken);
        return coverage.ToDto();
    }

    public async Task<CoverageDto> CreateAsync(
        CreateCoverageRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(name, null, cancellationToken);

        var coverage = new Coverage
        {
            Name = name,
            Description = request.Description?.Trim(),
            Rate = request.Rate,
            IsActive = request.IsActive
        };

        await _coverageRepository.AddAsync(coverage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return coverage.ToDto();
    }

    public async Task<CoverageDto> UpdateAsync(
        int id,
        UpdateCoverageRequest request,
        CancellationToken cancellationToken = default)
    {
        var coverage = await FindOrThrowAsync(id, cancellationToken);

        var name = request.Name.Trim();
        await EnsureNameIsAvailableAsync(name, id, cancellationToken);

        coverage.Name = name;
        coverage.Description = request.Description?.Trim();
        coverage.Rate = request.Rate;
        coverage.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return coverage.ToDto();
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var coverage = await FindOrThrowAsync(id, cancellationToken);

        if (!coverage.IsActive)
        {
            return;
        }

        coverage.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Coverage> FindOrThrowAsync(int id, CancellationToken cancellationToken) =>
        await _coverageRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new EntityNotFoundException("la cobertura", id);

    private async Task EnsureNameIsAvailableAsync(
        string name,
        int? excludeCoverageId,
        CancellationToken cancellationToken)
    {
        if (await _coverageRepository.ExistsByNameAsync(name, excludeCoverageId, cancellationToken))
        {
            throw new ConflictException(
                "COVERAGE_ALREADY_EXISTS",
                $"Ya existe una cobertura con el nombre '{name}'.");
        }
    }
}
