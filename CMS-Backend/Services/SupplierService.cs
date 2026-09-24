// Services/SupplierService.cs
using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;

namespace CMS_Backend.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SupplierService> _logger;

    private const int NameMaxLength = 150;
    private const int NotesMaxLength = 2000;
    private const int PhoneMaxLength = 20;
    private const int EmailMaxLength = 150;
    private const int TaxCardMaxLength = 50;
    private const int CommercialRegisterMaxLength = 50;
    private const int AddressMaxLength = 250;

    private static readonly Regex PhoneRegex = new(@"^[0-9+\-\s()]{6,20}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public SupplierService(
        AppDbContext dbContext,
        ILogger<SupplierService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let uniqueness checks exclude the record being updated.
    private async Task<string?> ValidateAsync(Supplier supplier, bool isUpdate, long? currentId = null)
    {
        if (supplier is null)
            return "Supplier payload is required";

        // --- Name (required + unique) ---
        if (string.IsNullOrWhiteSpace(supplier.Name))
            return "Supplier name is required";

        if (supplier.Name.Trim().Length > NameMaxLength)
            return $"Supplier name cannot exceed {NameMaxLength} characters";

        supplier.Name = supplier.Name.Trim();

        var nameQuery = _dbContext.Supplier
            .Where(s => !s.IsDeleted && s.Name.ToLower() == supplier.Name.ToLower());

        if (isUpdate && currentId.HasValue)
            nameQuery = nameQuery.Where(s => s.Id != currentId.Value);

        if (await nameQuery.AnyAsync())
            return $"A supplier named '{supplier.Name}' already exists";

        // --- NameEn (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(supplier.NameEn) && supplier.NameEn.Trim().Length > NameMaxLength)
            return $"NameEn cannot exceed {NameMaxLength} characters";

        if (!string.IsNullOrWhiteSpace(supplier.NameEn))
            supplier.NameEn = supplier.NameEn.Trim();

        // --- Phone (optional, format-checked if present) ---
        if (!string.IsNullOrWhiteSpace(supplier.Phone))
        {
            supplier.Phone = supplier.Phone.Trim();
            if (supplier.Phone.Length > PhoneMaxLength || !PhoneRegex.IsMatch(supplier.Phone))
                return "Phone is not a valid phone number";
        }

        // --- Email (optional, format-checked if present) ---
        if (!string.IsNullOrWhiteSpace(supplier.Email))
        {
            supplier.Email = supplier.Email.Trim();

            if (supplier.Email.Length > EmailMaxLength || !EmailRegex.IsMatch(supplier.Email))
                return "Email is not a valid email address";
        }

        // --- TaxCard (optional, unique if present) ---
        if (!string.IsNullOrWhiteSpace(supplier.TaxCard))
        {
            supplier.TaxCard = supplier.TaxCard.Trim();

            if (supplier.TaxCard.Length > TaxCardMaxLength)
                return $"TaxCard cannot exceed {TaxCardMaxLength} characters";

            var taxCardQuery = _dbContext.Supplier
                .Where(s => !s.IsDeleted && s.TaxCard == supplier.TaxCard);

            if (isUpdate && currentId.HasValue)
                taxCardQuery = taxCardQuery.Where(s => s.Id != currentId.Value);

            if (await taxCardQuery.AnyAsync())
                return $"A supplier with tax card '{supplier.TaxCard}' already exists";
        }

        // --- CommercialRegister (optional, unique if present) ---
        if (!string.IsNullOrWhiteSpace(supplier.CommercialRegister))
        {
            supplier.CommercialRegister = supplier.CommercialRegister.Trim();

            if (supplier.CommercialRegister.Length > CommercialRegisterMaxLength)
                return $"CommercialRegister cannot exceed {CommercialRegisterMaxLength} characters";

            var crQuery = _dbContext.Supplier
                .Where(s => !s.IsDeleted && s.CommercialRegister == supplier.CommercialRegister);

            if (isUpdate && currentId.HasValue)
                crQuery = crQuery.Where(s => s.Id != currentId.Value);

            if (await crQuery.AnyAsync())
                return $"A supplier with commercial register '{supplier.CommercialRegister}' already exists";
        }

        // --- City (required FK) ---
        if (supplier.CityId <= 0)
            return "CityId is required";

        var cityExists = await _dbContext.City.AnyAsync(c => c.Id == supplier.CityId);
        if (!cityExists)
            return $"City with Id {supplier.CityId} was not found";

        // --- Address (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(supplier.Address) && supplier.Address.Length > AddressMaxLength)
            return $"Address cannot exceed {AddressMaxLength} characters";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(supplier.Notes) && supplier.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        // --- OpeningBalance / FixedDiscount ---
        if (supplier.FixedDiscount < 0 || supplier.FixedDiscount > 100)
            return "FixedDiscount must be between 0 and 100";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<Supplier>>> GetAllAsync()
    {
        var suppliers = await _dbContext.Supplier
            .Where(z => !z.IsDeleted)
            .Include(s => s.City)
            .AsNoTracking()
            .ToListAsync();

        return ServiceResult<List<Supplier>>.Ok(suppliers);
    }

    public async Task<ServiceResult<Supplier?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<Supplier?>.Failed("A valid supplier Id is required");

        var supplier = await _dbContext.Supplier
            .Where(z => !z.IsDeleted && z.Id == id)
            .Include(s => s.City)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (supplier is null)
            return ServiceResult<Supplier?>.NotFound("Supplier not found");

        return ServiceResult<Supplier?>.Ok(supplier);
    }

    public async Task<ServiceResult<Supplier>> CreateAsync(Supplier supplier)
    {
        var validationError = await ValidateAsync(supplier, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating supplier: {Error}", validationError);
            return ServiceResult<Supplier>.Failed(validationError);
        }

        _dbContext.Supplier.Add(supplier);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Supplier created with Id {SupplierId}", supplier.Id);
        return ServiceResult<Supplier>.Ok(supplier);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, Supplier supplier)
    {
        if (supplier is null)
            return ServiceResult<bool>.Failed("Supplier payload is required");

        if (id != supplier.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Supplier.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Supplier not found");

        var validationError = await ValidateAsync(supplier, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating supplier {SupplierId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.Name = supplier.Name;
        existing.NameEn = supplier.NameEn;
        existing.Notes = supplier.Notes;
        existing.Phone = supplier.Phone;
        existing.Email = supplier.Email;
        existing.TaxCard = supplier.TaxCard;
        existing.CommercialRegister = supplier.CommercialRegister;
        existing.CityId = supplier.CityId;
        existing.Address = supplier.Address;
        existing.OpeningBalance = supplier.OpeningBalance;
        existing.FixedDiscount = supplier.FixedDiscount;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating supplier with Id {SupplierId}", id);
            throw;
        }

        _logger.LogInformation("Supplier with Id {SupplierId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var supplier = await _dbContext.Supplier.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (supplier is null)
            return ServiceResult<bool>.NotFound("Supplier not found");

        supplier.IsDeleted = true;
        supplier.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Supplier with Id {SupplierId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}