using System.ComponentModel.DataAnnotations;

namespace CMS.Application.DTOs;

public record UserListDto(string FullName, string Email, string userId);

public record ProfileDto(string FullName, string Email, string? PhoneNumber);

public record UpdateProfileDto(
    [Required, StringLength(100)] string FullName,
    [Phone, StringLength(30)] string? PhoneNumber);

public record ChangePasswordDto(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword);

public record ResetPasswordAdminDto(
    [Required] string UserId);
