using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;

public interface IEmployeeBalanceService
{
    Task<ServiceResult<EmployeeBalanceDto>> GetAsync(long employeeId, DateTime? from, DateTime? to);
    Task<ExportResult> ExportAsync(long employeeId, DateTime? from, DateTime? to);
}