using XeroProfitLossReport.Models;

namespace XeroProfitLossReport.Services
{
    public interface IXeroOAuthService
    {
        Task<XeroTokenResponse> ExchangeCodeForTokensAsync(string code, string redirectUri);
        string GenerateAuthorizationUrl(string state);
        string GenerateRandomState();
    }
}
