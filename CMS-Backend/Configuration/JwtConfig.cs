namespace CMS.Api.Configuration
{
    public class JwtConfig
    {
        public string Secret { get; set; }
        public System.TimeSpan ExpiryTimeFrame { get; set; }
    }
}
