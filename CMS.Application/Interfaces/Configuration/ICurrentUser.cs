namespace CMS.Application.Interfaces.Configuration;

public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? CorrelationId { get; }
    long? CurrentLogId { get; }
}
