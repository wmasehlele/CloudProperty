global using CloudProperty.Data;
global using Microsoft.EntityFrameworkCore;
using CloudProperty;
using CloudProperty.Sevices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<DatabaseContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddDistributedRedisCache(options =>
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

builder.Services.AddTransient<DataCache>();
builder.Services.AddTransient<BlobStorage>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
