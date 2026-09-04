using Lafise.Insurance.Application.Abstractions;
using Lafise.Insurance.Application.Contracts.Policies;
using Lafise.Insurance.Application.Mapping;
using Lafise.Insurance.Application.Options;
using Lafise.Insurance.Domain.Entities;
using Lafise.Insurance.Domain.Enums;
using Lafise.Insurance.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lafise.Insurance.Application.Services;

/// <summary>
/// Implementa la emisión de pólizas. Toda la validación de negocio y el cálculo de
/// la prima viven en esta capa; el controlador únicamente traduce HTTP.
/// </summary>
public class PolicyService : IPolicyService
{
    private const int PremiumDecimals = 2;

    private readonly IPolicyRepository _policyRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICoverageRepository _coverageRepository;
    private readonly IPolicyNumberGenerator _policyNumberGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPlateValidator _plateValidator;
    private readonly UnderwritingOptions _options;
    private readonly ILogger<PolicyService> _logger;

    public PolicyService(
        IPolicyRepository policyRepository,
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        ICoverageRepository coverageRepository,
        IPolicyNumberGenerator policyNumberGenerator,
        IPlateValidator plateValidator,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IOptions<UnderwritingOptions> options,
        ILogger<PolicyService> logger)
    {
        _policyRepository = policyRepository;
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _coverageRepository = coverageRepository;
        _policyNumberGenerator = policyNumberGenerator;
        _plateValidator = plateValidator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PolicyDetailDto> IssueAsync(
        IssuePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var issueDate = _dateTimeProvider.UtcNow;
        var plate = _plateValidator.NormalizeAndValidate(request.Vehicle.Plate);

        EnsureVehicleAgeIsAcceptable(request.Vehicle.Year, issueDate);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
                       ?? throw new EntityNotFoundException("el cliente", request.CustomerId);

        EnsureCustomerIsActive(customer);

        var coverages = await ResolveCoveragesAsync(request.CoverageIds, cancellationToken);

        if (await _policyRepository.HasActivePolicyForPlateAsync(customer.Id, plate, cancellationToken))
        {
            throw new ConflictException(
                "DUPLICATE_ACTIVE_POLICY",
                $"El cliente {customer.Name} ya tiene una póliza activa para la placa {plate}.");
        }

        var vehicle = await ResolveVehicleAsync(plate, request.Vehicle, issueDate, cancellationToken);

        var insuredAmount = decimal.Round(vehicle.CommercialValue, PremiumDecimals, MidpointRounding.AwayFromZero);

        var policy = new Policy
        {
            PolicyNumber = await _policyNumberGenerator.GenerateAsync(cancellationToken),
            Customer = customer,
            CustomerId = customer.Id,
            Vehicle = vehicle,
            VehicleId = vehicle.Id,
            IssueDate = issueDate,
            ExpirationDate = issueDate.AddMonths(_options.PolicyTermInMonths),
            InsuredAmount = insuredAmount,
            Status = PolicyStatus.Active,
            CreatedAtUtc = issueDate
        };

        policy.Coverages = BuildCoverageLines(coverages, insuredAmount);
        policy.TotalPremium = CalculateTotalPremium(policy.Coverages);

        await _policyRepository.AddAsync(policy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Póliza {PolicyNumber} emitida para el cliente {CustomerId} y la placa {Plate}. Prima total: {TotalPremium}.",
            policy.PolicyNumber,
            customer.Id,
            plate,
            policy.TotalPremium);

        return policy.ToDetailDto();
    }

    public async Task<PolicyDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(id, cancellationToken)
                     ?? throw new EntityNotFoundException("la póliza", id);

        return policy.ToDetailDto();
    }

    public async Task<IReadOnlyList<PolicySummaryDto>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var policies = await _policyRepository.GetAllAsync(cancellationToken);
        return policies.Select(policy => policy.ToSummaryDto()).ToList();
    }

    /// <summary>Regla: no se emiten pólizas a clientes dados de baja.</summary>
    private static void EnsureCustomerIsActive(Customer customer)
    {
        if (!customer.IsActive)
        {
            throw new BusinessRuleViolationException(
                "INACTIVE_CUSTOMER",
                $"El cliente '{customer.Name}' está dado de baja y no puede recibir nuevas pólizas.");
        }
    }

    /// <summary>Regla: no se emite si el vehículo supera la antigüedad máxima permitida.</summary>
    private void EnsureVehicleAgeIsAcceptable(int vehicleYear, DateTime issueDate)
    {
        if (vehicleYear > issueDate.Year + 1)
        {
            throw new BusinessRuleViolationException(
                "INVALID_VEHICLE_YEAR",
                $"El año del vehículo ({vehicleYear}) no puede ser posterior a {issueDate.Year + 1}.");
        }

        var age = issueDate.Year - vehicleYear;
        if (age > _options.MaxVehicleAgeInYears)
        {
            throw new BusinessRuleViolationException(
                "VEHICLE_TOO_OLD",
                $"No se puede emitir la póliza: el vehículo tiene {age} años de antigüedad y el máximo permitido es {_options.MaxVehicleAgeInYears}.");
        }
    }

    /// <summary>Valida que las coberturas existan, estén activas y no vengan duplicadas.</summary>
    private async Task<IReadOnlyList<Coverage>> ResolveCoveragesAsync(
        IReadOnlyCollection<int> coverageIds,
        CancellationToken cancellationToken)
    {
        if (coverageIds.Count == 0)
        {
            throw new BusinessRuleViolationException(
                "COVERAGES_REQUIRED",
                "Debe seleccionar al menos una cobertura para emitir la póliza.");
        }

        var requestedIds = coverageIds.Distinct().ToList();
        if (requestedIds.Count != coverageIds.Count)
        {
            throw new BusinessRuleViolationException(
                "DUPLICATED_COVERAGES",
                "La lista de coberturas contiene identificadores repetidos.");
        }

        var coverages = await _coverageRepository.GetByIdsAsync(requestedIds, cancellationToken);

        var missingIds = requestedIds.Except(coverages.Select(coverage => coverage.Id)).ToList();
        if (missingIds.Count > 0)
        {
            var formattedIds = string.Join(", ", missingIds);
            throw new EntityNotFoundException(
                "cobertura",
                formattedIds,
                $"No existen las siguientes coberturas: {formattedIds}.");
        }

        var inactiveNames = coverages.Where(coverage => !coverage.IsActive).Select(coverage => coverage.Name).ToList();
        if (inactiveNames.Count > 0)
        {
            throw new BusinessRuleViolationException(
                "INACTIVE_COVERAGE",
                $"Las siguientes coberturas no están disponibles: {string.Join(", ", inactiveNames)}.");
        }

        return coverages;
    }

    /// <summary>
    /// Reutiliza el vehículo si la placa ya está registrada (actualizando su valuación)
    /// o lo da de alta cuando es la primera vez que se asegura.
    /// </summary>
    private async Task<Vehicle> ResolveVehicleAsync(
        string normalizedPlate,
        VehicleRequest request,
        DateTime issueDate,
        CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByPlateAsync(normalizedPlate, cancellationToken);

        if (vehicle is null)
        {
            vehicle = new Vehicle
            {
                Plate = normalizedPlate,
                Brand = request.Brand.Trim(),
                Model = request.Model.Trim(),
                Year = request.Year,
                CommercialValue = request.CommercialValue,
                CreatedAtUtc = issueDate
            };

            await _vehicleRepository.AddAsync(vehicle, cancellationToken);
            return vehicle;
        }

        vehicle.Brand = request.Brand.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.Year = request.Year;
        vehicle.CommercialValue = request.CommercialValue;

        // Asegurar de nuevo una placa dada de baja la vuelve a poner en circulación;
        // de lo contrario un vehículo desactivado quedaría inasegurable para siempre.
        vehicle.IsActive = true;

        return vehicle;
    }

    /// <summary>
    /// Calcula el aporte de cada cobertura: SumaAsegurada * (Tasa / 100), redondeado a dos decimales.
    /// </summary>
    private static List<PolicyCoverage> BuildCoverageLines(IEnumerable<Coverage> coverages, decimal insuredAmount) =>
        coverages
            .Select(coverage => new PolicyCoverage
            {
                Coverage = coverage,
                CoverageId = coverage.Id,
                AppliedRate = coverage.Rate,
                PremiumAmount = decimal.Round(
                    insuredAmount * (coverage.Rate / 100m),
                    PremiumDecimals,
                    MidpointRounding.AwayFromZero)
            })
            .ToList();

    /// <summary>Prima total = suma de las primas por cobertura, nunca menor a la prima mínima.</summary>
    private decimal CalculateTotalPremium(IEnumerable<PolicyCoverage> coverageLines)
    {
        var total = coverageLines.Sum(line => line.PremiumAmount);
        return Math.Max(decimal.Round(total, PremiumDecimals, MidpointRounding.AwayFromZero), _options.MinimumPremium);
    }
}
