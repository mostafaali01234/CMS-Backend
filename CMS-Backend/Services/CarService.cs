// Services/CarService.cs
using CMS.Api.Data;
using CMS.Domain.Models;
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CMS.Api.Services;

public class CarService : ICarService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CarService> _logger;

    private const int NameMaxLength = 150;
    private const int NotesMaxLength = 2000;
    private const int PlatNumberMaxLength = 50;
    private const int ModelMaxLength = 100;
    private const int InsuranceCompanyNameMaxLength = 200;
    private const int OwnerNameMaxLength = 200;
    private const int MotorNumberMaxLength = 100;
    private const int ChassisNumberMaxLength = 100;

    public CarService(
        AppDbContext dbContext,
        ILogger<CarService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private CarDto ToDto(Car car)
    {
        return new CarDto
        {
            Id = car.Id,
            Name = car.Name,
            Notes = car.Notes,
            DriverId = car.DriverId,
            DriverName = car.Driver?.Name ?? "",
            PlatNumber = car.PlatNumber,
            Model = car.Model,
            OilChangeRate = car.OilChangeRate,
            FilterChangeRate = car.FilterChangeRate,
            SkChangeRate = car.SkChangeRate,
            TireChangeRate = car.TireChangeRate,
            LicenseStartDate = car.LicenseStartDate,
            LicenseEndDate = car.LicenseEndDate,
            InsuranceStartDate = car.InsuranceStartDate,
            InsuranceEndDate = car.InsuranceEndDate,
            InsuranceCompanyName = car.InsuranceCompanyName,
            OwnerName = car.OwnerName,
            MotorNumber = car.MotorNumber,
            ChassisNumber = car.ChassisNumber,
            CreatedAtUtc = car.CreatedAtUtc,
            CreatedBy = car.CreatedBy ?? "",
            UpdatedAtUtc = car.UpdatedAtUtc,
            UpdatedBy = car.UpdatedBy ?? ""
        };
    }

    private Car ToEntity(CarDto dto)
    {
        return new Car
        {
            Id = dto.Id,
            Name = dto.Name,
            Notes = dto.Notes,
            DriverId = dto.DriverId,
            PlatNumber = dto.PlatNumber,
            Model = dto.Model,
            OilChangeRate = dto.OilChangeRate,
            FilterChangeRate = dto.FilterChangeRate,
            SkChangeRate = dto.SkChangeRate,
            TireChangeRate = dto.TireChangeRate,
            LicenseStartDate = dto.LicenseStartDate,
            LicenseEndDate = dto.LicenseEndDate,
            InsuranceStartDate = dto.InsuranceStartDate,
            InsuranceEndDate = dto.InsuranceEndDate,
            InsuranceCompanyName = dto.InsuranceCompanyName,
            OwnerName = dto.OwnerName,
            MotorNumber = dto.MotorNumber,
            ChassisNumber = dto.ChassisNumber
        };
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let uniqueness checks exclude the record being updated.
    private async Task<string?> ValidateAsync(CarDto dto, bool isUpdate, long? currentId = null)
    {
        if (dto is null)
            return "Car payload is required";

        // --- Name (required) ---
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Car name is required";

        if (dto.Name.Trim().Length > NameMaxLength)
            return $"Car name cannot exceed {NameMaxLength} characters";

        dto.Name = dto.Name.Trim();

        // --- Driver (required FK) ---
        if (dto.DriverId <= 0)
            return "DriverId is required";

        var driverExists = await _dbContext.Employee
            .AnyAsync(e => e.Id == dto.DriverId && !e.IsDeleted);

        if (!driverExists)
            return $"Driver (Employee) with Id {dto.DriverId} was not found";

        // --- PlatNumber (required + unique) ---
        if (string.IsNullOrWhiteSpace(dto.PlatNumber))
            return "PlatNumber is required";

        if (dto.PlatNumber.Trim().Length > PlatNumberMaxLength)
            return $"PlatNumber cannot exceed {PlatNumberMaxLength} characters";

        dto.PlatNumber = dto.PlatNumber.Trim();

        var platQuery = _dbContext.Car
            .Where(c => !c.IsDeleted && c.PlatNumber.ToLower() == dto.PlatNumber.ToLower());

        if (isUpdate && currentId.HasValue)
            platQuery = platQuery.Where(c => c.Id != currentId.Value);

        if (await platQuery.AnyAsync())
            return $"A car with plate number '{dto.PlatNumber}' already exists";

        // --- Model (required) ---
        if (string.IsNullOrWhiteSpace(dto.Model))
            return "Model is required";

        if (dto.Model.Trim().Length > ModelMaxLength)
            return $"Model cannot exceed {ModelMaxLength} characters";

        dto.Model = dto.Model.Trim();

        // --- Maintenance rates (non-negative) ---
        if (dto.OilChangeRate < 0)
            return "OilChangeRate cannot be negative";

        if (dto.FilterChangeRate < 0)
            return "FilterChangeRate cannot be negative";

        if (dto.SkChangeRate < 0)
            return "SkChangeRate cannot be negative";

        if (dto.TireChangeRate < 0)
            return "TireChangeRate cannot be negative";

        // --- License dates ---
        if (dto.LicenseStartDate == default)
            return "LicenseStartDate is required";

        if (dto.LicenseEndDate == default)
            return "LicenseEndDate is required";

        if (dto.LicenseEndDate.Date <= dto.LicenseStartDate.Date)
            return "LicenseEndDate must be after LicenseStartDate";

        // --- Insurance dates ---
        if (dto.InsuranceStartDate == default)
            return "InsuranceStartDate is required";

        if (dto.InsuranceEndDate == default)
            return "InsuranceEndDate is required";

        if (dto.InsuranceEndDate.Date <= dto.InsuranceStartDate.Date)
            return "InsuranceEndDate must be after InsuranceStartDate";

        // --- InsuranceCompanyName (required) ---
        if (string.IsNullOrWhiteSpace(dto.InsuranceCompanyName))
            return "InsuranceCompanyName is required";

        if (dto.InsuranceCompanyName.Trim().Length > InsuranceCompanyNameMaxLength)
            return $"InsuranceCompanyName cannot exceed {InsuranceCompanyNameMaxLength} characters";

        dto.InsuranceCompanyName = dto.InsuranceCompanyName.Trim();

        // --- OwnerName (required) ---
        if (string.IsNullOrWhiteSpace(dto.OwnerName))
            return "OwnerName is required";

        if (dto.OwnerName.Trim().Length > OwnerNameMaxLength)
            return $"OwnerName cannot exceed {OwnerNameMaxLength} characters";

        dto.OwnerName = dto.OwnerName.Trim();

        // --- MotorNumber (required + unique) ---
        if (string.IsNullOrWhiteSpace(dto.MotorNumber))
            return "MotorNumber is required";

        if (dto.MotorNumber.Trim().Length > MotorNumberMaxLength)
            return $"MotorNumber cannot exceed {MotorNumberMaxLength} characters";

        dto.MotorNumber = dto.MotorNumber.Trim();

        var motorQuery = _dbContext.Car
            .Where(c => !c.IsDeleted && c.MotorNumber.ToLower() == dto.MotorNumber.ToLower());

        if (isUpdate && currentId.HasValue)
            motorQuery = motorQuery.Where(c => c.Id != currentId.Value);

        if (await motorQuery.AnyAsync())
            return $"A car with motor number '{dto.MotorNumber}' already exists";

        // --- ChassisNumber (required + unique) ---
        if (string.IsNullOrWhiteSpace(dto.ChassisNumber))
            return "ChassisNumber is required";

        if (dto.ChassisNumber.Trim().Length > ChassisNumberMaxLength)
            return $"ChassisNumber cannot exceed {ChassisNumberMaxLength} characters";

        dto.ChassisNumber = dto.ChassisNumber.Trim();

        var chassisQuery = _dbContext.Car
            .Where(c => !c.IsDeleted && c.ChassisNumber.ToLower() == dto.ChassisNumber.ToLower());

        if (isUpdate && currentId.HasValue)
            chassisQuery = chassisQuery.Where(c => c.Id != currentId.Value);

        if (await chassisQuery.AnyAsync())
            return $"A car with chassis number '{dto.ChassisNumber}' already exists";

        // --- Notes (optional, length only) ---
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<CarDto>>> GetAllAsync()
    {
        var cars = await _dbContext.Car
            .Where(z => !z.IsDeleted)
            .Include(c => c.Driver)
            .AsNoTracking()
            .ToListAsync();

        var dtos = cars.Select(ToDto).ToList();
        return ServiceResult<List<CarDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<CarDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<CarDto?>.Failed("A valid car Id is required");

        var car = await _dbContext.Car
            .Where(z => !z.IsDeleted && z.Id == id)
            .Include(c => c.Driver)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (car is null)
            return ServiceResult<CarDto?>.NotFound("Car not found");

        return ServiceResult<CarDto?>.Ok(ToDto(car));
    }

    public async Task<ServiceResult<CarDto>> CreateAsync(CarDto dto)
    {
        var validationError = await ValidateAsync(dto, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating car: {Error}", validationError);
            return ServiceResult<CarDto>.Failed(validationError);
        }

        var car = ToEntity(dto);

        _dbContext.Car.Add(car);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Car created with Id {CarId}", car.Id);

        var created = await _dbContext.Car
            .Include(c => c.Driver)
            .AsNoTracking()
            .FirstAsync(c => c.Id == car.Id);

        return ServiceResult<CarDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, CarDto dto)
    {
        if (dto is null)
            return ServiceResult<bool>.Failed("Car payload is required");

        if (id != dto.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Car.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Car not found");

        var validationError = await ValidateAsync(dto, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating car {CarId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }

        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.Name = dto.Name;
        existing.Notes = dto.Notes;
        existing.DriverId = dto.DriverId;
        existing.PlatNumber = dto.PlatNumber;
        existing.Model = dto.Model;
        existing.OilChangeRate = dto.OilChangeRate;
        existing.FilterChangeRate = dto.FilterChangeRate;
        existing.SkChangeRate = dto.SkChangeRate;
        existing.TireChangeRate = dto.TireChangeRate;
        existing.LicenseStartDate = dto.LicenseStartDate;
        existing.LicenseEndDate = dto.LicenseEndDate;
        existing.InsuranceStartDate = dto.InsuranceStartDate;
        existing.InsuranceEndDate = dto.InsuranceEndDate;
        existing.InsuranceCompanyName = dto.InsuranceCompanyName;
        existing.OwnerName = dto.OwnerName;
        existing.MotorNumber = dto.MotorNumber;
        existing.ChassisNumber = dto.ChassisNumber;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating car with Id {CarId}", id);
            throw;
        }

        _logger.LogInformation("Car with Id {CarId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var car = await _dbContext.Car.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (car is null)
            return ServiceResult<bool>.NotFound("Car not found");

        car.IsDeleted = true;
        car.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Car with Id {CarId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}