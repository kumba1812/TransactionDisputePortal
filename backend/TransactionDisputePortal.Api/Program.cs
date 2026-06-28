using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using TransactionDisputePortal.Api.Data;
using TransactionDisputePortal.Api.Filters;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Api.Repositories;
using TransactionDisputePortal.Api.Repositories.User;
using TransactionDisputePortal.Api.Integration;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/app-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    Log.Information("Starting TransactionDisputePortal API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the logging pipeline
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers(options =>
    {
        // Register global exception filter
        options.Filters.Add<GlobalExceptionFilter>();
    })
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

    // Configure DbContext: Postgres when connection string indicates, else SQLite
    var conn = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(conn) && conn.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(conn));
        Log.Information("Using PostgreSQL database");
    }
    else
    {
        Log.Warning("No valid PostgreSQL database connection string found");
    }

    // CORS: allow frontend origins in Development, restrict in Production
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                Log.Information("CORS configured for Development: AllowAnyOrigin");
            }
            else
            {
                policy.WithOrigins("https://your-production-frontend.example.com")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
                Log.Information("CORS configured for Production");
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
        Log.Information("Swagger UI enabled");
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
            Log.Information("Attempting to apply database migrations");
            dbContext.Database.Migrate();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database migrations failed, falling back to EnsureCreated");
            dbContext.Database.EnsureCreated();
        }
    }

    Log.Information("TransactionDisputePortal API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
