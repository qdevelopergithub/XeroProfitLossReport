using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using XeroProfitLossReport.Models;

namespace XeroProfitLossReport.Services
{
    public class XeroOAuthService : IXeroOAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly XeroConfiguration _configuration;
        private readonly ILogger<XeroOAuthService> _logger;

        public XeroOAuthService(HttpClient httpClient, IOptions<XeroConfiguration> configuration, ILogger<XeroOAuthService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration.Value;
            _logger = logger;
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

        public async Task<XeroTokenResponse> ExchangeCodeForTokensAsync(string code, string redirectUri)
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
                using JsonDocument doc = JsonDocument.Parse(responseContent);
                string accessToken = doc.RootElement.GetProperty("access_token").GetString();


                _logger.LogInformation("Token response received. Status: {StatusCode}, Content length: {ContentLength}", 
                    response.StatusCode, responseContent?.Length ?? 0);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to exchange code for tokens. Status: {StatusCode}, Content: {Content}", 
                        response.StatusCode, responseContent);
                    throw new HttpRequestException($"Token exchange failed with status {response.StatusCode}: {responseContent}");
                }

                _logger.LogInformation("Raw token response: {ResponseContent}", responseContent);

                var tokenResponse = JsonSerializer.Deserialize<XeroTokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (tokenResponse == null)
                {
                    _logger.LogError("Failed to deserialize token response. Raw content: {Content}", responseContent);
                    throw new InvalidOperationException($"Failed to deserialize token response: {responseContent}");
                }

                _logger.LogInformation("Successfully exchanged code for tokens. Token type: {TokenType}, Expires in: {ExpiresIn}", 
                    tokenResponse.TokenType, tokenResponse.ExpiresIn);
                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging code for tokens. Code: {Code}, RedirectUri: {RedirectUri}", code, redirectUri);
                throw;
            }
        }
    }
}
