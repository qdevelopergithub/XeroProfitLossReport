using XeroProfitLossReport.Models;
using XeroProfitLossReport.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Xero OAuth
builder.Services.Configure<XeroConfiguration>(builder.Configuration.GetSection("Xero"));

// Register services
builder.Services.AddHttpClient<IXeroOAuthService, XeroOAuthService>();
builder.Services.AddScoped<IXeroOAuthService, XeroOAuthService>();

var app = builder.Build();

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
