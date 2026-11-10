using CMS.Application.DTOs;
using CMS.Application.DTOs.Responses;
using CMS.Application.Helpers;

namespace CMS.Application.Interfaces;

public interface IProjectReviewService
{
    // one summary row per project (the list page)
    Task<ServiceResult<List<ProjectReviewSummaryDto>>> GetSummariesAsync(ProjectReviewFilterDto f);
    // summary + every record tied to the project
    Task<ServiceResult<ProjectReviewDto>> GetReviewAsync(long projectId, ProjectReviewFilterDto f);
    Task<ExportResult> ExportAsync(long projectId, ProjectReviewFilterDto f);
}
