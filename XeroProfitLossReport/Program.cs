using Microsoft.EntityFrameworkCore;
using XeroProfitLossReport.Data;
using XeroProfitLossReport.Models;
using XeroProfitLossReport.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Xero OAuth
builder.Services.Configure<XeroConfiguration>(builder.Configuration.GetSection("Xero"));

// Register services
builder.Services.AddHttpClient<IXeroOAuthService, XeroOAuthService>();
builder.Services.AddScoped<IXeroOAuthService, XeroOAuthService>();

var app = builder.Build();

// Create database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serve static files
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
