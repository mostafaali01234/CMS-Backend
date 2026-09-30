namespace CMS.Application.DTOs
{
    public class JwtConfig
    {
        public string Secret { get; set; }
        public System.TimeSpan ExpiryTimeFrame { get; set; }
    }
}
