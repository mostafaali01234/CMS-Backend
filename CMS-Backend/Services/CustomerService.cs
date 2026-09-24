// Services/CustomerService.cs
using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;

namespace CMS_Backend.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CustomerService> _logger;

    private const int NameMaxLength = 150;
    private const int NotesMaxLength = 2000;
    private const int PhoneMaxLength = 20;
    private const int AddressMaxLength = 250;

    private static readonly Regex PhoneRegex = new(@"^[0-9+\-\s()]{6,20}$", RegexOptions.Compiled);

    public CustomerService(
        AppDbContext dbContext,
        ILogger<CustomerService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private CustomerDto ToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Notes = customer.Notes,
            Phone = customer.Phone,
            Phone2 = customer.Phone2,
            Phone3 = customer.Phone3,
            CityId = customer.CityId,
            CityName = customer.City?.Name ?? "",
            Address = customer.Address,
            OpeningBalance = customer.OpeningBalance,
            SellerId = customer.SellerId,
            SellerName = customer.Seller?.UserName ?? "",
            RegularCustomer = customer.RegularCustomer,
            CreatedAtUtc = customer.CreatedAtUtc,
            CreatedBy = customer.CreatedBy ?? "",
            UpdatedAtUtc = customer.UpdatedAtUtc,
            UpdatedBy = customer.UpdatedBy ?? ""
        };
    }

    private Customer ToEntity(CustomerDto dto)
    {
        return new Customer
        {
            Id = dto.Id,
            Name = dto.Name,
            Notes = dto.Notes,
            Phone = dto.Phone,
            Phone2 = dto.Phone2,
            Phone3 = dto.Phone3,
            CityId = dto.CityId,
            Address = dto.Address,
            OpeningBalance = dto.OpeningBalance,
            SellerId = dto.SellerId,
            RegularCustomer = dto.RegularCustomer
        };
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let uniqueness checks exclude the record being updated.
    private async Task<string?> ValidateAsync(CustomerDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Customer payload is required";

        // --- Name (required) ---
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Customer name is required";

        if (dto.Name.Trim().Length > NameMaxLength)
            return $"Customer name cannot exceed {NameMaxLength} characters";

        dto.Name = dto.Name.Trim();

        // --- Phone (required + unique) ---
        if (string.IsNullOrWhiteSpace(dto.Phone))
            return "Phone is required";

        dto.Phone = dto.Phone.Trim();

        if (dto.Phone.Length > PhoneMaxLength || !PhoneRegex.IsMatch(dto.Phone))
            return "Phone is not a valid phone number";

        var phoneQuery = _dbContext.Customer
            .Where(c => !c.IsDeleted && c.Phone == dto.Phone);

        if (isUpdate && currentId.HasValue)
            phoneQuery = phoneQuery.Where(c => c.Id != currentId.Value);

        if (await phoneQuery.AnyAsync())
            return $"A customer with phone '{dto.Phone}' already exists";

        // --- Phone2 / Phone3 (optional, format-checked if present) ---
        if (!string.IsNullOrWhiteSpace(dto.Phone2))
        {
            dto.Phone2 = dto.Phone2.Trim();
            if (dto.Phone2.Length > PhoneMaxLength || !PhoneRegex.IsMatch(dto.Phone2))
                return "Phone2 is not a valid phone number";
        }

        if (!string.IsNullOrWhiteSpace(dto.Phone3))
        {
            dto.Phone3 = dto.Phone3.Trim();
            if (dto.Phone3.Length > PhoneMaxLength || !PhoneRegex.IsMatch(dto.Phone3))
                return "Phone3 is not a valid phone number";
        }

        // --- City (required FK) ---
        if (dto.CityId <= 0)
            return "CityId is required";

        var cityExists = await _dbContext.City.AnyAsync(c => c.Id == dto.CityId);
        if (!cityExists)
            return $"City with Id {dto.CityId} was not found";

        // --- Address (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Address) && dto.Address.Length > AddressMaxLength)
            return $"Address cannot exceed {AddressMaxLength} characters";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        // --- OpeningBalance --- (no bound; can legitimately be negative, e.g. a credit balance)

        // --- Seller (required FK to IdentityUser) ---
        if (string.IsNullOrWhiteSpace(dto.SellerId))
            return "SellerId is required";

        var sellerExists = await _dbContext.Users.AnyAsync(u => u.Id == dto.SellerId);
        if (!sellerExists)
            return $"Seller with Id {dto.SellerId} was not found";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<CustomerDto>>> GetAllAsync()
    {
        var customers = await _dbContext.Customer
            .Where(z => !z.IsDeleted)
            .Include(c => c.City)
            .Include(c => c.Seller)
            .AsNoTracking()
            .ToListAsync();

        var dtos = customers.Select(ToDto).ToList();
        return ServiceResult<List<CustomerDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<CustomerDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<CustomerDto?>.Failed("A valid customer Id is required");

        var customer = await _dbContext.Customer
            .Where(z => !z.IsDeleted && z.Id == id)
            .Include(c => c.City)
            .Include(c => c.Seller)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (customer is null)
            return ServiceResult<CustomerDto?>.NotFound("Customer not found");

        return ServiceResult<CustomerDto?>.Ok(ToDto(customer));
    }

    public async Task<ServiceResult<CustomerDto>> CreateAsync(CustomerDto dto)
    {
        var validationError = await ValidateAsync(dto, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating customer: {Error}", validationError);
            return ServiceResult<CustomerDto>.Failed(validationError);
        }

        var customer = ToEntity(dto);

        _dbContext.Customer.Add(customer);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Customer created with Id {CustomerId}", customer.Id);

        var created = await _dbContext.Customer
            .Include(c => c.City)
            .Include(c => c.Seller)
            .AsNoTracking()
            .FirstAsync(c => c.Id == customer.Id);

        return ServiceResult<CustomerDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, CustomerDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Customer payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Customer.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Customer not found");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating customer {CustomerId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.Name = dto.Name;
        existing.Notes = dto.Notes;
        existing.Phone = dto.Phone;
        existing.Phone2 = dto.Phone2;
        existing.Phone3 = dto.Phone3;
        existing.CityId = dto.CityId;
        existing.Address = dto.Address;
        existing.OpeningBalance = dto.OpeningBalance;
        existing.SellerId = dto.SellerId;
        existing.RegularCustomer = dto.RegularCustomer;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating customer with Id {CustomerId}", id);
            throw;
        }

        _logger.LogInformation("Customer with Id {CustomerId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var customer = await _dbContext.Customer.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (customer is null)
            return ServiceResult<bool>.NotFound("Customer not found");

        customer.IsDeleted = true;
        customer.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Customer with Id {CustomerId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}