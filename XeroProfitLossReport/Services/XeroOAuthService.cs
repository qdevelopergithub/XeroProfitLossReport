using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using XeroProfitLossReport.Data;
using XeroProfitLossReport.Models;

namespace XeroProfitLossReport.Services
{
    public class XeroOAuthService : IXeroOAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly XeroConfiguration _configuration;
        private readonly ILogger<XeroOAuthService> _logger;
        private readonly AppDbContext _context;

        public XeroOAuthService(HttpClient httpClient, IOptions<XeroConfiguration> configuration, ILogger<XeroOAuthService> logger, AppDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration.Value;
            _logger = logger;
            _context = context;
        }

        public string GenerateAuthorizationUrl(string state)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = _configuration.ClientId,
                ["redirect_uri"] = _configuration.RedirectUri,
                ["scope"] = "openid profile email offline_access accounting.transactions accounting.reports.read accounting.settings accounting.journals.read accounting.contacts.read accounting.attachments",
                ["state"] = state
            };

            var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            return $"{_configuration.AuthorizationUrl}?{queryString}";
        }

        public string GenerateRandomState()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        public async Task<OAuthResponse> ExchangeCodeForTokensAsync(string code, string redirectUri)
        {
            try
            {
                _logger.LogInformation("Starting token exchange. Code length: {CodeLength}, RedirectUri: {RedirectUri}",
                    code?.Length ?? 0, redirectUri);

                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_configuration.ClientId}:{_configuration.ClientSecret}"));

                var request = new HttpRequestMessage(HttpMethod.Post, _configuration.TokenUrl);
                request.Headers.Add("Authorization", $"Basic {credentials}");
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri)
                });

                _logger.LogInformation("Sending token request to: {TokenUrl}", _configuration.TokenUrl);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                OAuthResponse oAuthResponse = new OAuthResponse();
                // Save OAuth response to database
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(responseContent);
                    oAuthResponse = new OAuthResponse
                    {
                        AccessToken = doc.RootElement.GetProperty("access_token").GetString() ?? "",
                        IdToken = doc.RootElement.GetProperty("id_token").GetString() ?? "",
                        RefreshToken = doc.RootElement.GetProperty("refresh_token").GetString() ?? "",
                        ExpiresIn = doc.RootElement.GetProperty("expires_in").GetInt32(),
                        TokenType = doc.RootElement.GetProperty("token_type").GetString() ?? "",
                        Scope = doc.RootElement.GetProperty("scope").GetString() ?? "",
                        CreatedAt = DateTime.UtcNow
                    };

                    // Remove all existing tokens first (keep only 1 token)
                    var existingTokens = _context.OAuthResponses.ToList();
                    _context.OAuthResponses.RemoveRange(existingTokens);
                    
                    // Add new token
                    _context.OAuthResponses.Add(oAuthResponse);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("OAuth response updated in database successfully (replaced existing token)");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save OAuth response to database");
                }
                return oAuthResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging code for tokens. Code: {Code}, RedirectUri: {RedirectUri}", code, redirectUri);
                throw;
            }
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                var oauthResponse = await _context.OAuthResponses.FirstOrDefaultAsync();
                return oauthResponse?.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving access token from database");
                return null;
            }
        }
    }
}
