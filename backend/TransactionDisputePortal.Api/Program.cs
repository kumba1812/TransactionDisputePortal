using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TransactionDisputePortal.Api.Data;
using TransactionDisputePortal.Api.Models;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Repositories;
using TransactionDisputePortal.Api.Repositories.User;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Configure DbContext: Postgres when connection string indicates, else SQLite
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(conn) && conn.Contains("Host=", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(conn));
}
else
{
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

// Repository + Identity password hasher registration
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IDisputeRepository, DisputeRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();

// Authentication — JWT Bearer
var jwtKey    = builder.Configuration["Jwt:Key"]    ?? Environment.GetEnvironmentVariable("JWT_KEY")    ?? "ChangeMeInProductionKey123!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "TransactionDisputePortal";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BankerOrAbove", p => p.RequireRole("Admin", "Banker"));
    options.AddPolicy("ClientOrAbove", p => p.RequireRole("Admin", "Client"));
    options.AddPolicy("WriteAccess",   p => p.RequireRole("Admin"));
});

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TransactionDisputePortal API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without 'Bearer ' prefix)"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TransactionDisputePortal API V1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

// Authentication & Authorization middleware
app.UseAuthentication();


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
