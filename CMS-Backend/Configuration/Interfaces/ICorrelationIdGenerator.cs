namespace CMS.Api.Configuration.Interfaces;

public interface ICorrelationIdGenerator
{
    string Get();
    void Set(string correlationId);
}
