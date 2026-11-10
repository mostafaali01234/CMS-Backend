// Services/Interfaces/IEmployeePayrollService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;
using CMS.Domain.DTOs;

namespace CMS.Application.Interfaces;

public interface IEmployeePayrollService
{
    Task<ServiceResult<List<EmployeePayrollDto>>> GetAllAsync();
    Task<ServiceResult<EmployeePayrollDto?>> GetByIdAsync(long id);
    Task<ServiceResult<List<EmployeePayrollDto>>> GetByEmployeeIdAsync(long employeeId);
    Task<ServiceResult<EmployeePayrollDto?>> GetByEmployeeIdMonthYearAsync(long employeeId, int month, int year);
    Task<ServiceResult<EmployeePayrollDto>> CreateAsync(EmployeePayrollDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, EmployeePayrollDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);

    Task<ServiceResult<PayrollPreviewDto>> PreviewAsync(long employeeId, int month, int year);
    Task<ServiceResult<EmployeePayrollDto>> ReviewAsync(PayrollReviewRequestDto req);
    Task<ServiceResult<bool>> WithdrawAsync(long id, PayrollWithdrawRequestDto req);
    Task<ServiceResult<PagedResult<EmployeePayrollDto>>> GetPagedAsync(PayrollFilterDto f);
    Task<ServiceResult<bool>> SetCommissionExtraAsync(long commissionId, decimal extra);
    Task<ServiceResult<EmployeeAdjustmentDto>> AddAdjustmentAsync(EmployeeAdjustmentDto dto);
    Task<ServiceResult<bool>> DeleteAdjustmentAsync(long id);

    Task<ServiceResult<RecalcResultDto>> RecalculateCommissionsAsync(long employeeId, int month, int year);
    Task<ExportResult> ExportReviewAsync(long employeeId, int month, int year);
}