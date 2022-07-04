global using CloudProperty.Data;
global using Microsoft.EntityFrameworkCore;
using CloudProperty;
using CloudProperty.Sevices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using NLog;
using NLog.Web;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Application init");

builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("ClickaTell", httpClient =>
{
	httpClient.BaseAddress = new Uri("https://platform.clickatell.com/");
	httpClient.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.Accept, "application/json");
	httpClient.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.Authorization, builder.Configuration.GetSection("ClickatellSettings:Key").Value);
});


builder.Services.AddControllers();

builder.Services.AddDbContext<DatabaseContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = builder.Configuration.GetConnectionString("CacheConnection");
	options.InstanceName = "master";

});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuerSigningKey = true,
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
		.GetBytes(builder.Configuration.GetSection("AppSettings:secrete").Value)),
		ValidateIssuer = false,
		ValidateAudience = false
	};
});

builder.Services.Configure<MailSettingsService>(builder.Configuration.GetSection("MailSettings"));

builder.Services.AddTransient<LoggerService>();
builder.Services.AddTransient<ExceptionMiddleware>();
builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<DataCacheService>();
builder.Services.AddTransient<FileStorageService>();
builder.Services.AddTransient<LookupTokenService>();
builder.Services.AddTransient<CommunicationService>();
builder.Services.AddTransient<TemplateService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	logger.Info($"App running on environment: {app.Environment.EnvironmentName}");
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
