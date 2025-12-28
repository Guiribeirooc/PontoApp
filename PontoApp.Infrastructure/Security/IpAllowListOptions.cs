namespace PontoApp.Infrastructure.Security
{
    public sealed class IpAllowListOptions
    {
        public bool Enforce { get; set; } = false;
        public string[] Allowed { get; set; } = Array.Empty<string>();
    }
}
