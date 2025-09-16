using XeroProfitLossReport.Models;

namespace XeroProfitLossReport.Services
{
    public interface IXeroOAuthService
    {
        Task<OAuthResponse> ExchangeCodeForTokensAsync(string code, string redirectUri);
        string GenerateAuthorizationUrl(string state);
        string GenerateRandomState();
        Task<string?> GetAccessTokenAsync();
    }
}
