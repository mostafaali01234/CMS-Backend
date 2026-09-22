using CMS_Backend.Data;
using CMS_Backend.Models;
using CMS_Backend.Models.DTOs;
using CMS_Backend.Models.DTOs.Responses;
using CMS_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;
namespace CMS_Backend.Services;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<EmployeeService> _logger;

    private const int NameMaxLength = 150;
    private const int IdentificationNumberMaxLength = 50;
    private const int PhoneMaxLength = 20;
    private const int NotesMaxLength = 2000;
    private const int AdditionalAddressMaxLength = 250;
    private const int EducationMaxLength = 150;
    private static readonly Regex PhoneRegex = new(@"^[0-9+\-\s()]{6,20}$", RegexOptions.Compiled);

    public EmployeeService(
        AppDbContext dbContext,
        ILogger<EmployeeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private List<EmployeeDto> convertToDto(List<Employee> emps)
    {
        var dtos = new List<EmployeeDto>();

        foreach (var emp in emps)
        {
            dtos.Add(new EmployeeDto
            {
                Id = emp.Id,
                Name = emp.Name,
                Active = emp.Active,
                CityId = emp.CityId,
                CityName = emp.CityAddress?.Name ?? "",
                DepartmentId = emp.DepartmentId ?? 0,
                departmentName = emp.Department?.Name ?? "",
                UserId = emp.UserId,
                UserEmail = emp.UserAccount?.Email ?? "",
                AdditionalAddress = emp.AdditionalAddress,
                BirthDate = emp.BirthDate,
                Education = emp.Education,
                EducationSpec = emp.EducationSpec,
                HireDate = emp.HireDate,
                IdentificationNumber = emp.IdentificationNumber,
                JobId = emp.JobId,
                JobName = emp.Job?.Name ?? "",
                KidsCount = emp.KidsCount,
                MartialStatus = emp.MartialStatus,
                MilitaryStatus = emp.MilitaryStatus,
                Notes = emp.Notes,
                OpeningBalance = emp.OpeningBalance,
                PersonalPhone = emp.PersonalPhone,
                Salary = emp.Salary,
                WorkPhone = emp.WorkPhone,

                CreatedAtUtc = emp.CreatedAtUtc,
                CreatedBy = emp.CreatedBy ?? "",
                UpdatedAtUtc = emp.UpdatedAtUtc,
                UpdatedBy = emp.UpdatedBy ?? ""
            });
        }

        return dtos;
    }
    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
            age--;
        return age;
    }

    // Centralized validation used by both Create and Update.
    // isUpdate/currentId let uniqueness checks exclude the record being updated.
    private async Task<string?> ValidateAsync(Employee employee, bool isUpdate, long? currentId = null)
    {
        if (employee is null)
            return "Employee payload is required";

        // --- Name ---
        if (string.IsNullOrWhiteSpace(employee.Name))
            return "Employee name is required";

        if (employee.Name.Trim().Length > NameMaxLength)
            return $"Employee name cannot exceed {NameMaxLength} characters";

        employee.Name = employee.Name.Trim();

        // --- Job (required FK) ---
        if (employee.JobId <= 0)
            return "JobId is required";

        var jobExists = await _dbContext.Job.AnyAsync(j => j.Id == employee.JobId && !j.IsDeleted);
        if (!jobExists)
            return $"Job with Id {employee.JobId} was not found";

        // --- Department (optional FK) ---
        if (employee.DepartmentId.HasValue)
        {
            var departmentExists = await _dbContext.Department
                .AnyAsync(d => d.Id == employee.DepartmentId.Value && !d.IsDeleted);

            if (!departmentExists)
                return $"Department with Id {employee.DepartmentId.Value} was not found";
        }

        // --- City (required FK) ---
        if (employee.CityId <= 0)
            return "CityId is required";

        var cityExists = await _dbContext.City.AnyAsync(c => c.Id == employee.CityId);
        if (!cityExists)
            return $"City with Id {employee.CityId} was not found";

        // --- Salary / OpeningBalance ---
        if (employee.Salary < 0)
            return "Salary cannot be negative";

        if (employee.OpeningBalance < 0)
            return "OpeningBalance cannot be negative";

        // --- HireDate ---
        if (employee.HireDate == default)
            return "HireDate is required";

        //if (employee.HireDate.Date > DateTime.UtcNow.Date)
        //    return "HireDate cannot be in the future";

        // --- BirthDate (optional) ---
        if (employee.BirthDate.HasValue)
        {
            if (employee.BirthDate.Value.Date > DateTime.UtcNow.Date)
                return "BirthDate cannot be in the future";

            var age = CalculateAge(employee.BirthDate.Value);
            if (age < 16 || age > 100)
                return "BirthDate results in an implausible age (must be between 16 and 100 years old)";

            if (employee.BirthDate.Value.Date > employee.HireDate.Date)
                return "BirthDate cannot be after HireDate";
        }

        // --- IdentificationNumber (required + unique) ---
        if (string.IsNullOrWhiteSpace(employee.IdentificationNumber))
            return "IdentificationNumber is required";

        if (employee.IdentificationNumber.Trim().Length > IdentificationNumberMaxLength)
            return $"IdentificationNumber cannot exceed {IdentificationNumberMaxLength} characters";

        employee.IdentificationNumber = employee.IdentificationNumber.Trim();

        var idQuery = _dbContext.Employee
            .Where(e => !e.IsDeleted && e.IdentificationNumber == employee.IdentificationNumber);

        if (isUpdate && currentId.HasValue)
            idQuery = idQuery.Where(e => e.Id != currentId.Value);

        if (await idQuery.AnyAsync())
            return $"An employee with identification number '{employee.IdentificationNumber}' already exists";

        // --- UserId (optional, but if present must exist and be unique per employee) ---
        if (!string.IsNullOrWhiteSpace(employee.UserId))
        {
            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == employee.UserId);
            if (!userExists)
                return $"User account with Id {employee.UserId} was not found";

            var userLinkQuery = _dbContext.Employee
                .Where(e => !e.IsDeleted && e.UserId == employee.UserId);

            if (isUpdate && currentId.HasValue)
                userLinkQuery = userLinkQuery.Where(e => e.Id != currentId.Value);

            if (await userLinkQuery.AnyAsync())
                return "This user account is already linked to another employee";
        }

        // --- Phones (optional, format-checked if present) ---
        if (!string.IsNullOrWhiteSpace(employee.PersonalPhone))
        {
            employee.PersonalPhone = employee.PersonalPhone.Trim();
            if (employee.PersonalPhone.Length > PhoneMaxLength || !PhoneRegex.IsMatch(employee.PersonalPhone))
                return "PersonalPhone is not a valid phone number";
        }

        if (!string.IsNullOrWhiteSpace(employee.WorkPhone))
        {
            employee.WorkPhone = employee.WorkPhone.Trim();
            if (employee.WorkPhone.Length > PhoneMaxLength || !PhoneRegex.IsMatch(employee.WorkPhone))
                return "WorkPhone is not a valid phone number";
        }

        // --- Enums ---
        if (!Enum.IsDefined(typeof(MilitaryStatus), employee.MilitaryStatus))
            return "MilitaryStatus is not a valid value";

        if (!Enum.IsDefined(typeof(MartialStatus), employee.MartialStatus))
            return "MartialStatus is not a valid value";

        // --- KidsCount ---
        if (employee.KidsCount.HasValue && employee.KidsCount.Value < 0)
            return "KidsCount cannot be negative";

        // --- Free-text length guards ---
        if (!string.IsNullOrWhiteSpace(employee.Notes) && employee.Notes.Length > NotesMaxLength)
            return $"Notes cannot exceed {NotesMaxLength} characters";

        if (!string.IsNullOrWhiteSpace(employee.AdditionalAddress) && employee.AdditionalAddress.Length > AdditionalAddressMaxLength)
            return $"AdditionalAddress cannot exceed {AdditionalAddressMaxLength} characters";

        if (!string.IsNullOrWhiteSpace(employee.Education) && employee.Education.Length > EducationMaxLength)
            return $"Education cannot exceed {EducationMaxLength} characters";

        if (!string.IsNullOrWhiteSpace(employee.EducationSpec) && employee.EducationSpec.Length > EducationMaxLength)
            return $"EducationSpec cannot exceed {EducationMaxLength} characters";

        return null; // no validation errors
    }

    public async Task<ServiceResult<List<EmployeeDto>>> GetAllAsync()
    {
        var emps = await _dbContext.Employee
            .Include(z => z.CityAddress)
            .Include(z => z.Job)
            .Include(z => z.Department)
            .Include(z => z.UserAccount)
            .Where(z => !z.IsDeleted).AsNoTracking().ToListAsync();
        return ServiceResult<List<EmployeeDto>>.Ok(convertToDto(emps));
    }

    public async Task<ServiceResult<EmployeeDto?>> GetByIdAsync(long id)
    {
        if (id <= 0)
            return ServiceResult<EmployeeDto?>.Failed("A valid employee Id is required");

        var emp = await _dbContext.Employee
            .Include(z => z.CityAddress)
            .Include(z => z.Job)
            .Include(z => z.Department)
            .Include(z => z.UserAccount)
            .Where(z => !z.IsDeleted && z.Id == id).AsNoTracking().ToListAsync();

        var dto = convertToDto(emp).FirstOrDefault();
        if (dto is null)
            return ServiceResult<EmployeeDto?>.NotFound("Employee not found");

        return ServiceResult<EmployeeDto?>.Ok(dto);
    }

    public async Task<ServiceResult<EmployeeDto>> CreateAsync(Employee emp)
    {
        var validationError = await ValidateAsync(emp, isUpdate: false);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while creating Employee: {Error}", validationError);
            return ServiceResult<EmployeeDto>.Failed(validationError);
        }

        _dbContext.Employee.Add(emp);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Employee created with Id {EmployeeId}", emp.Id);

        // Reload with Manager included so ManagerName is populated on the DTO
        var created = await _dbContext.Employee
            .Include(z => z.CityAddress)
            .Include(z => z.Job)
            .Include(z => z.Department)
            .Include(z => z.UserAccount)
            .AsNoTracking()
            .FirstAsync(d => d.Id == emp.Id);

        return ServiceResult<EmployeeDto>.Ok(convertToDto(new List<Employee> { created }).First());
    }

    public async Task<ServiceResult<bool>> UpdateAsync(long id, Employee employee)
    {
        if (employee is null)
            return ServiceResult<bool>.Failed("Employee payload is required");

        if (id != employee.Id)
            return ServiceResult<bool>.Failed("Id in the route does not match the Id in the payload");

        var existing = await _dbContext.Employee.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (existing is null)
            return ServiceResult<bool>.NotFound("Employee not found");

        var validationError = await ValidateAsync(employee, isUpdate: true, currentId: id);
        if (validationError is not null)
        {
            _logger.LogWarning("Validation failed while updating employee {EmployeeId}: {Error}", id, validationError);
            return ServiceResult<bool>.Failed(validationError);
        }


        // Update mutable fields explicitly rather than blindly overwriting audit/soft-delete fields
        existing.Name = employee.Name;
        existing.JobId = employee.JobId;
        existing.DepartmentId = employee.DepartmentId;
        existing.Salary = employee.Salary;
        existing.OpeningBalance = employee.OpeningBalance;
        existing.HireDate = employee.HireDate;
        existing.Active = employee.Active;
        existing.UserId = employee.UserId;
        existing.Notes = employee.Notes;
        existing.BirthDate = employee.BirthDate;
        existing.IdentificationNumber = employee.IdentificationNumber;
        existing.CityId = employee.CityId;
        existing.AdditionalAddress = employee.AdditionalAddress;
        existing.MilitaryStatus = employee.MilitaryStatus;
        existing.MartialStatus = employee.MartialStatus;
        existing.KidsCount = employee.KidsCount;
        existing.PersonalPhone = employee.PersonalPhone;
        existing.WorkPhone = employee.WorkPhone;
        existing.Education = employee.Education;
        existing.EducationSpec = employee.EducationSpec;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error occurred while updating employee with Id {EmployeeId}", id);
            throw;
        }

        _logger.LogInformation("Employee with Id {EmployeeId} updated successfully", id);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(long id)
    {
        var employee = await _dbContext.Employee.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (employee is null)
            return ServiceResult<bool>.NotFound("Employee not found");

        // Guard against deleting an employee who is currently set as a Department's manager
        var managesDepartment = await _dbContext.Department
            .AnyAsync(d => !d.IsDeleted && d.ManagerId == id);

        if (managesDepartment)
            return ServiceResult<bool>.Failed("Cannot delete an employee who is currently assigned as a department manager");

        employee.IsDeleted = true;
        employee.DeletedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Employee with Id {EmployeeId} soft-deleted successfully", id);
        return ServiceResult<bool>.Ok(true);
    }
}