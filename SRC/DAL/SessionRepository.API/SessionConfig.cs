namespace DAL.API
{
    using Services.API;

    [ConfigNode("Session")]
    public class SessionConfig
    {
        public int TimeoutInMinutes { get; set; }
    }
}
