using System.Text.Json.Serialization;

namespace CMS.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]  
    public enum TechType { Tech, Half_Tech, Tech_And_Helper, Helper }
}
