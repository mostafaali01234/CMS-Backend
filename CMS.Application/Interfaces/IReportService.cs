using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;

namespace CMS.Application.Interfaces;

public interface IReportService
{
    // 1 - orders that are not done but already have a sale invoice
    Task<ServiceResult<List<UnfinishedOrderRowDto>>> GetUnfinishedOrdersAsync(ReportDateFilterDto f);
    Task<ExportResult> ExportUnfinishedOrdersAsync(ReportDateFilterDto f);

    // 2 - orders with an advance payment that is not attached to an invoice
    Task<ServiceResult<List<AdvanceOrderRowDto>>> GetAdvanceOrdersAsync(ReportDateFilterDto f);
    Task<ExportResult> ExportAdvanceOrdersAsync(ReportDateFilterDto f);

    // 3 - sale invoices with a remaining amount
    Task<ServiceResult<List<UnpaidInvoiceRowDto>>> GetUnpaidInvoicesAsync(ReportDateFilterDto f);
    Task<ExportResult> ExportUnpaidInvoicesAsync(ReportDateFilterDto f);

    // 4 - net sales per seller per month (6 months)
    Task<ServiceResult<CrossTabDto>> GetMonthlySalesAsync(MonthlySalesFilterDto f);
    Task<ExportResult> ExportMonthlySalesAsync(MonthlySalesFilterDto f);

    // 5 - 10 - state / seller / tech (rows) x product category (columns), for orders or sale invoices
    Task<ServiceResult<CrossTabDto>> GetCategoryMatrixAsync(ReportSource source, ReportRowBy rowBy, CategoryMatrixFilterDto f);
    Task<ExportResult> ExportCategoryMatrixAsync(ReportSource source, ReportRowBy rowBy, CategoryMatrixFilterDto f);

    // 11 - maintenance orders created within 30 days of their original order, per tech
    Task<ServiceResult<MaintenanceReportDto>> GetMaintenanceAsync(ReportDateFilterDto f);
    Task<ExportResult> ExportMaintenanceAsync(ReportDateFilterDto f);
}
