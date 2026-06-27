using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionDisputePortal.Api.Data;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Configure DbContext: support by default, Postgres when connection string indicates
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(conn) && conn.Contains("Host=", StringComparison.OrdinalIgnoreCase))
{
    // Assume Postgres connection string
    builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(conn));
}
else
{
    // Log Error no database configured
    Console.WriteLine("No valid database connection string found.");
}

// CORS: allow frontend origins in Development, restrict in Production
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins("https://your-production-frontend.example.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// Repository registration
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IDisputeRepository, DisputeRepository>();

// Authentication - JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY") ?? "ChangeMeInProductionKey123!";
    var issuer = builder.Configuration["Jwt:Issuer"] ?? "TransactionDisputePortal";

    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = false
    };
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TransactionDisputePortal API V1");
        options.RoutePrefix = "swagger"; // serve at /swagger
    });
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

// Authentication & Authorization middleware
app.UseAuthentication();

// In Development, if no authentication present, inject a demo user for convenience
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (!context.User?.Identity?.IsAuthenticated ?? false)
        {
            var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1") };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Development");
            context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        }
        await next();
    });
}

app.UseAuthorization();

app.MapControllers();

// Apply EF Core migrations if available, otherwise fall back to EnsureCreated
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception)
    {
        // If migrations are not present or migration application fails, fallback to EnsureCreated to keep developer experience
        dbContext.Database.EnsureCreated();
    }
}

app.Run();
