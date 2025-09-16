using Microsoft.AspNetCore.Mvc;
using XeroProfitLossReport.Services;
using XeroProfitLossReport.Models;

namespace XeroProfitLossReport.Controllers
{
    [ApiController]
    [Route("oauth")]
    public class OAuthController : ControllerBase
    {
        private readonly IXeroOAuthService _oauthService;
        private readonly ILogger<OAuthController> _logger;

        // Store state temporarily for validation (in production, use distributed cache or database)
        private static readonly Dictionary<string, DateTime> _stateStore = new();

        public OAuthController(IXeroOAuthService oauthService, ILogger<OAuthController> logger)
        {
            _oauthService = oauthService;
            _logger = logger;
        }

        /// <summary>
        /// Initiates the OAuth flow by redirecting user to Xero authorization page
        /// </summary>
        [HttpGet("authorize")]
        public IActionResult Authorize()
        {
            try
            {
                var state = _oauthService.GenerateRandomState();
                
                // Store state with timestamp for validation (expires in 10 minutes)
                _stateStore[state] = DateTime.UtcNow.AddMinutes(10);
                
                var authorizationUrl = _oauthService.GenerateAuthorizationUrl(state);
                
                _logger.LogInformation("Redirecting to Xero authorization URL");
                return Redirect(authorizationUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating OAuth flow");
                return BadRequest(new { error = "Failed to initiate OAuth flow" });
            }
        }

        /// <summary>
        /// Handles the callback from Xero after user authorization
        /// </summary>
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
        {
            try
            {
                // Check for errors from Xero
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning("OAuth error received: {Error}", error);
                    return BadRequest(new { error = $"Authorization failed: {error}" });
                }

                // Validate required parameters
                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogWarning("Authorization code not received");
                    return BadRequest(new { error = "Authorization code not received" });
                }

                if (string.IsNullOrEmpty(state))
                {
                    _logger.LogWarning("State parameter not received");
                    return BadRequest(new { error = "State parameter not received" });
                }

                // Validate state to prevent CSRF attacks
                if (!_stateStore.TryGetValue(state, out var stateTimestamp))
                {
                    _logger.LogWarning("Invalid or expired state parameter");
                    return BadRequest(new { error = "Invalid or expired state parameter" });
                }

                if (stateTimestamp < DateTime.UtcNow)
                {
                    _stateStore.Remove(state);
                    _logger.LogWarning("Expired state parameter");
                    return BadRequest(new { error = "Expired state parameter" });
                }

                // Remove used state
                _stateStore.Remove(state);

                // Exchange code for tokens
                var redirectUri = Url.Action("Callback", "OAuth", null, Request.Scheme) ?? "";
                var tokenResponse = await _oauthService.ExchangeCodeForTokensAsync(code, redirectUri);

                _logger.LogInformation("Successfully completed OAuth flow for user");

                // Return success response with token information
                return Ok(new
                {
                    message = "OAuth flow completed successfully",
                    //access_token = tokenResponse.AccessToken,
                    //token_type = tokenResponse.TokenType,
                    //expires_in = tokenResponse.ExpiresIn,
                    //scope = tokenResponse.Scope,
                    //// Note: In production, don't return tokens in response. Store them securely and use session/auth cookies
                    //id_token = tokenResponse.IdToken,
                    //refresh_token = tokenResponse.RefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OAuth callback. Code: {Code}, State: {State}, Error: {Error}", code, state, error);
                return BadRequest(new { 
                    error = "Failed to complete OAuth flow", 
                    details = ex.Message,
                    code = code?.Substring(0, Math.Min(10, code?.Length ?? 0)) + "...",
                    state = state?.Substring(0, Math.Min(10, state?.Length ?? 0)) + "..."
                });
            }
        }

        /// <summary>
        /// Get OAuth status and configuration info (for testing)
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                message = "OAuth endpoints are ready",
                authorize_url = Url.Action("Authorize", "OAuth", null, Request.Scheme),
                callback_url = Url.Action("Callback", "OAuth", null, Request.Scheme)
            });
        }

        /// <summary>
        /// Get access token from database
        /// </summary>
        [HttpGet("access-token")]
        public async Task<IActionResult> GetAccessToken()
        {
            try
            {
                var accessToken = await _oauthService.GetAccessTokenAsync();

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("No access token found in database");
                    return NotFound(new { error = "No access token found" });
                }

                _logger.LogInformation("Access token retrieved successfully");

                return Ok(new
                {
                    message = "Access token retrieved successfully",
                    access_token = accessToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving access token");
                return StatusCode(500, new { error = "Failed to retrieve access token", details = ex.Message });
            }
        }
    }
}
