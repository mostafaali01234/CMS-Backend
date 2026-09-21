using CMS_Backend.Configuration.Interfaces;

namespace CMS_Backend.Configuration;

public class CorrelationIdGenerator : ICorrelationIdGenerator
{
    private string _correlationId = Guid.NewGuid().ToString();
    public string Get() => _correlationId;

    public void Set(string correlationId) => _correlationId = correlationId;
}
