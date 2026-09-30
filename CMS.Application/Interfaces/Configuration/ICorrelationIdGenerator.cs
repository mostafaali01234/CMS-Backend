namespace CMS.Application.Interfaces.Configuration;

public interface ICorrelationIdGenerator
{
    string Get();
    void Set(string correlationId);
}
