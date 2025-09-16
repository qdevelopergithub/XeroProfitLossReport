namespace XeroProfitLossReport.Models
{
    public class XeroAuthRequest
    {
        public string ResponseType { get; set; } = "code";
        public string ClientId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid profile email accounting.transactions";
        public string State { get; set; } = string.Empty;
    }
}
