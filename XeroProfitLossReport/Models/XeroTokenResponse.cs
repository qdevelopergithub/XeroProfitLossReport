namespace XeroProfitLossReport.Models
{
    public class XeroTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string IdToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
        public string[] Scope { get; set; } = Array.Empty<string>();
    }
}
