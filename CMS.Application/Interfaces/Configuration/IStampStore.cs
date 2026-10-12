namespace CMS.Application.Interfaces.Configuration;

public interface IStampStore
{
    Task<string?> GetAsync(string userId);
    Task InvalidateAsync(string userId);
}
