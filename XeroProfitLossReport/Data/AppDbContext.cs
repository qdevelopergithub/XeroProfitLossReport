using Microsoft.EntityFrameworkCore;
using XeroProfitLossReport.Models;

namespace XeroProfitLossReport.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<OAuthResponse> OAuthResponses { get; set; }
    }
}
