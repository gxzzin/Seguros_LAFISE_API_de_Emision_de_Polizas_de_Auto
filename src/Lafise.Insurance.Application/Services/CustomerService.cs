using Lafise.Insurance.Application.Abstractions;
using Lafise.Insurance.Application.Contracts.Customers;
using Lafise.Insurance.Application.Mapping;
using Lafise.Insurance.Domain.Entities;
using Lafise.Insurance.Domain.Exceptions;

namespace Lafise.Insurance.Application.Services;

/// <summary>CRUD del catálogo de clientes.</summary>
public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(bool onlyActive, CancellationToken cancellationToken = default);

    Task<CustomerDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>Baja lógica del cliente. No elimina la fila para conservar el historial.</summary>
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ICustomerService"/>
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly IIdentificationValidator _identificationValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CustomerService(
        ICustomerRepository customerRepository,
        IPolicyRepository policyRepository,
        IIdentificationValidator identificationValidator,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _customerRepository = customerRepository;
        _policyRepository = policyRepository;
        _identificationValidator = identificationValidator;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(
        bool onlyActive,
        CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(onlyActive, cancellationToken);
        return customers.Select(customer => customer.ToDto()).ToList();
    }

    public async Task<CustomerDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await FindOrThrowAsync(id, cancellationToken);
        return customer.ToDto();
    }

    public async Task<CustomerDto> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var identification = _identificationValidator.NormalizeAndValidate(request.IdentificationNumber);
        await EnsureIdentificationIsAvailableAsync(identification, null, cancellationToken);

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            IdentificationNumber = identification,
            Email = request.Email.Trim(),
            IsActive = true,
            CreatedAtUtc = _dateTimeProvider.UtcNow
        };

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToDto();
    }

    public async Task<CustomerDto> UpdateAsync(
        int id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await FindOrThrowAsync(id, cancellationToken);

        var identification = _identificationValidator.NormalizeAndValidate(request.IdentificationNumber);
        await EnsureIdentificationIsAvailableAsync(identification, id, cancellationToken);

        // Desactivar por PUT queda sujeto a la misma regla que el DELETE.
        if (customer.IsActive && !request.IsActive)
        {
            await EnsureHasNoActivePoliciesAsync(customer, cancellationToken);
        }

        customer.Name = request.Name.Trim();
        customer.IdentificationNumber = identification;
        customer.Email = request.Email.Trim();
        customer.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToDto();
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await FindOrThrowAsync(id, cancellationToken);

        if (!customer.IsActive)
        {
            return;
        }

        await EnsureHasNoActivePoliciesAsync(customer, cancellationToken);

        customer.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Customer> FindOrThrowAsync(int id, CancellationToken cancellationToken) =>
        await _customerRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new EntityNotFoundException("el cliente", id);

    private async Task EnsureIdentificationIsAvailableAsync(
        string identification,
        int? excludeCustomerId,
        CancellationToken cancellationToken)
    {
        if (await _customerRepository.ExistsByIdentificationAsync(identification, excludeCustomerId, cancellationToken))
        {
            throw new ConflictException(
                "CUSTOMER_ALREADY_EXISTS",
                $"Ya existe un cliente registrado con la identificación '{identification}'.");
        }
    }

    private async Task EnsureHasNoActivePoliciesAsync(Customer customer, CancellationToken cancellationToken)
    {
        if (await _policyRepository.HasActivePoliciesForCustomerAsync(customer.Id, cancellationToken))
        {
            throw new ConflictException(
                "CUSTOMER_HAS_ACTIVE_POLICIES",
                $"No se puede dar de baja al cliente '{customer.Name}' porque tiene pólizas activas.");
        }
    }
}
