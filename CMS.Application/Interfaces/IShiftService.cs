// Services/Interfaces/IShiftService.cs
using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Domain.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Interfaces;

public interface IShiftService
{
    Task<ServiceResult<List<ShiftDto>>> GetAllAsync();
    Task<ServiceResult<List<ShiftDto>>> GetAllByDateAsync(DateTime date);
    Task<ServiceResult<ShiftDto?>> GetByIdAsync(long id);
    Task<ServiceResult<ShiftDto?>> GetByStoreIdAndDateAsync(long storeId, DateTime date);
    Task<ServiceResult<ShiftDto>> CreateAsync(ShiftDto dto);
    Task<ServiceResult<bool>> UpdateAsync(long id, ShiftDto dto);
    Task<ServiceResult<bool>> DeleteAsync(long id);
    Task<ServiceResult<ShiftBoardDto>> GetBoardAsync(long storeId, DateTime date);
    Task<ServiceResult<ExpenseDto>> AddExpenseAsync(long shiftId, ExpenseDto dto);
    Task<ServiceResult<EmployeeLoanDto>> AddLoanAsync(long shiftId, EmployeeLoanDto dto);
    Task<ServiceResult<SaleInvoiceDto>> AddInvoiceAsync(long shiftId, SaleInvoiceDto dto);
    Task<ServiceResult<MoneySafeTransactionDto>> AddSettlementAsync(long shiftId, ShiftTransferRequestDto r);
    Task<ServiceResult<MoneySafeTransactionDto>> AddCustodyAsync(long shiftId, ShiftTransferRequestDto r);
    Task<ServiceResult<bool>> DeleteTransactionAsync(long shiftId, long transactionId);

}