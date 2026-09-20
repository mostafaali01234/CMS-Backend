namespace CMS_Backend.Configuration
{
    public class JwtConfig
    {
        public string Secret { get; set; }
        public System.TimeSpan ExpiryTimeFrame { get; set; }
    }
}
