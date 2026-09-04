using Lafise.Insurance.Application.Contracts.Coverages;
using Lafise.Insurance.Application.Services;
using Lafise.Insurance.Domain.Entities;
using Lafise.Insurance.Domain.Exceptions;
using Lafise.Insurance.UnitTests.TestDoubles;
using Xunit;

namespace Lafise.Insurance.UnitTests;

/// <summary>Pruebas del CRUD de coberturas.</summary>
public class CoverageServiceTests
{
    private readonly InMemoryStore _store = new();
    private readonly CoverageService _service;

    public CoverageServiceTests()
    {
        _store.Coverages.AddRange(new[]
        {
            new Coverage { Id = 1, Name = "Robo", Rate = 2.50m, IsActive = true },
            new Coverage { Id = 2, Name = "Choque", Rate = 3.25m, IsActive = true },
            new Coverage { Id = 6, Name = "Asistencia Vial", Rate = 0.40m, IsActive = false }
        });

        _service = new CoverageService(new FakeCoverageRepository(_store), new FakeUnitOfWork(_store));
    }

    [Fact]
    public async Task GetAllAsync_WhenOnlyActive_OmitsDiscontinuedCoverages()
    {
        var coverages = await _service.GetAllAsync(onlyActive: true);

        Assert.Equal(2, coverages.Count);
        Assert.DoesNotContain(coverages, coverage => coverage.Id == 6);
    }

    [Fact]
    public async Task CreateAsync_AddsTheCoverageToTheCatalog()
    {
        var created = await _service.CreateAsync(new CreateCoverageRequest
        {
            Name = "Daños por Inundación",
            Description = "Cubre daños por crecidas e inundaciones.",
            Rate = 1.40m,
            IsActive = true
        });

        Assert.True(created.Id > 0);
        Assert.Equal(1.40m, created.Rate);
        Assert.Equal(4, _store.Coverages.Count);
    }

    [Fact]
    public async Task CreateAsync_WithAnExistingName_ThrowsConflict()
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(new CreateCoverageRequest { Name = "Robo", Rate = 1m }));

        Assert.Equal("COVERAGE_ALREADY_EXISTS", exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_ChangesTheRateInTheCatalog()
    {
        var updated = await _service.UpdateAsync(1, new UpdateCoverageRequest
        {
            Name = "Robo",
            Rate = 2.90m,
            IsActive = true
        });

        Assert.Equal(2.90m, updated.Rate);
        Assert.Equal(2.90m, _store.Coverages.Single(coverage => coverage.Id == 1).Rate);
    }

    [Fact]
    public async Task UpdateAsync_WithTheNameOfAnotherCoverage_ThrowsConflict()
    {
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateAsync(1, new UpdateCoverageRequest { Name = "Choque", Rate = 2.50m }));

        Assert.Equal("COVERAGE_ALREADY_EXISTS", exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_CanReactivateADiscontinuedCoverage()
    {
        var updated = await _service.UpdateAsync(6, new UpdateCoverageRequest
        {
            Name = "Asistencia Vial",
            Rate = 0.45m,
            IsActive = true
        });

        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WhenCoverageDoesNotExist_ThrowsEntityNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _service.UpdateAsync(404, new UpdateCoverageRequest { Name = "Nueva", Rate = 1m }));
    }

    [Fact]
    public async Task DeactivateAsync_DiscontinuesTheCoverageWithoutDeletingTheRow()
    {
        await _service.DeactivateAsync(1);

        Assert.Equal(3, _store.Coverages.Count);
        Assert.False(_store.Coverages.Single(coverage => coverage.Id == 1).IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_OnAnAlreadyDiscontinuedCoverage_IsIdempotent()
    {
        await _service.DeactivateAsync(6);

        Assert.Equal(0, _store.SaveChangesCallCount);
    }

    [Fact]
    public async Task DeactivateAsync_WhenCoverageDoesNotExist_ThrowsEntityNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.DeactivateAsync(404));
    }
}
