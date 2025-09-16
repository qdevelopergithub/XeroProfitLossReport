using System.ComponentModel.DataAnnotations;

namespace XeroProfitLossReport.Models
{
    public class OAuthResponse
    {
        [Key]
        public int Id { get; set; }
        
        public string AccessToken { get; set; } = string.Empty;
        public string IdToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
