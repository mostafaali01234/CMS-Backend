using CMS_Backend.Models.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CMS_Backend.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // Success -> 200 with the data as the body. Failure -> RFC 7807 ProblemDetails.
    protected IActionResult ToActionResult<T>(ServiceResult<T> result) =>
        result.Succeeded ? Ok(result) : ToProblem(result);

    // For commands that have nothing useful to return (add/remove) -> 204.
    protected IActionResult ToNoContentResult<T>(ServiceResult<T> result) =>
        result.Succeeded ? NoContent() : ToProblem(result);

    private IActionResult ToProblem<T>(ServiceResult<T> result) =>
        Problem(
            title: result.Status switch
            {
                ResultStatus.NotFound => "Not found",
                ResultStatus.Conflict => "Conflict",
                ResultStatus.Unauthorized => "Unauthorized",
                _ => "Request failed"
            },
            detail: string.Join("; ", result.Errors),
            statusCode: result.Status switch
            {
                ResultStatus.NotFound => StatusCodes.Status404NotFound,
                ResultStatus.Conflict => StatusCodes.Status409Conflict,
                ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status400BadRequest
            });
}