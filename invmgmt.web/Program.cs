using invmgmt.web.Data;
using invmgmt.web.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using invmgmt.web.Utils;
using invmgmt.web.Repositories;
using invmgmt.web.Services;
using invmgmt.web.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Kestrel – listen on every interface, HTTP port 5001
// ---------------------------------------------------------------
builder.WebHost.ConfigureKestrel(options =>
{
    // Bind to 0.0.0.0:5001  (accessible from any NIC)
    options.ListenAnyIP(5001);
    // When you have a cert you can enable HTTPS on 5001:
    // options.ListenAnyIP(5001, o => o.UseHttps("path/to/cert.pfx", "password"));
});
builder.Configuration.AddEnvironmentVariables(); // Load env vars, including Docker secrets
var config = builder.Configuration;

builder.Host.UseSerilog((ctx, services, serilogConfig) =>
    serilogConfig
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

// =======================
// SERVICES
// =======================

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB with connection pooling and resilience
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("DefaultConnection");

    // ── Production guard ────────────────────────────────────────────────────
    // Connection string must come from appsettings.Production.json (gitignored)
    // or the ConnectionStrings__DefaultConnection environment variable.
    // It must never be empty or point to localhost in production.
    if (string.IsNullOrWhiteSpace(connString))
        throw new InvalidOperationException(
            "[STARTUP] DefaultConnection is empty. " +
            "Set the ConnectionStrings__DefaultConnection environment variable " +
            "or populate appsettings.Production.json (gitignored).");

    // Log which host is being used so misconfiguration is obvious at boot
    var host = connString
        .Split(';', StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault(p => p.TrimStart().StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
        ?.Split('=', 2).LastOrDefault() ?? "unknown";

    Console.WriteLine($"[STARTUP] ✓ Database host: {host}");

    options.UseNpgsql(connString, npgsqlOptions =>
    {
        // Log connection string details (without password)
        var connStringForLog = System.Text.RegularExpressions.Regex.Replace(
            connString, 
            @"Password=[^;]+", 
            "Password=***", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        Console.WriteLine($"[STARTUP] Connection String: {connStringForLog}");
        
        // Connection resilience settings
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay:TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
        
        // Add timeout settings
        npgsqlOptions.ProvideClientCertificatesCallback(null);
    });
});

// =======================
// IDENTITY & CUSTOM SERVICES
// =======================
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<ISectionWiseQueryService, SectionWiseQueryService>();

// Memory cache for lightweight server-side caching in production
builder.Services.AddMemoryCache();

// Personnel Management
builder.Services.AddScoped<invmgmt.web.Repositories.IPersonnelRepository, invmgmt.web.Repositories.PersonnelRepository>();
builder.Services.AddScoped<invmgmt.web.Services.IPersonnelService, invmgmt.web.Services.PersonnelService>();
// =======================
// CORS (FIXED)
// =======================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// =======================
// JWT
// =======================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    var jwtKey = config["Jwt:Key"] ?? "THIS_IS_MY_SECRET_KEY_12345";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = config["Jwt:Issuer"],
        ValidAudience = config["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// =======================
// PIPELINE
// =======================

// Initialize database with retry logic
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        int maxRetries = 5;
        int delayMs = 2000;
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                logger.LogInformation($"[DB Init] Attempting to connect to database (attempt {i + 1}/{maxRetries})...");
                
                // Check for pending model changes before migrating
                var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation($"[DB Init] Found {pendingMigrations.Count()} pending migrations. Applying them now...");
                }
                
                // Run migrations
                await db.Database.MigrateAsync();
                logger.LogInformation("[DB Init] ✓ Database migrated successfully.");
                break;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("pending model changes"))
            {
                logger.LogWarning($"[DB Init] EF Core pending model changes detected. This is OK in development.");
                // This is expected if model hasn't been scaffolded yet - continue anyway
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[DB Init] Database connection failed: {ex.Message}");
                if (i < maxRetries - 1)
                {
                    logger.LogInformation($"[DB Init] Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, 10000); // Exponential backoff, max 10s
                }
                else
                {
                    logger.LogError($"[DB Init] Failed to connect after {maxRetries} attempts. Continuing anyway...");
                }
            }
        }

        // Step 1: Seed Roles
        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Id = 1, Name = "User" },
                new Role { Id = 2, Name = "Issuer" },
                new Role { Id = 3, Name = "Admin" }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("[DB Init] Roles seeded.");
        }

        // Step 2: Seed Departments
        if (!await db.Departments.AnyAsync())
        {
            db.Departments.AddRange(
                new Department { Id = 1, Name = "Admin" },
                new Department { Id = 2, Name = "IT" },
                new Department { Id = 3, Name = "HR" },
                new Department { Id = 4, Name = "Finance" }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("[DB Init] Departments seeded.");
        }

        // Step 3: Seed Categories
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Name = "Stationary" },
                new Category { Name = "IT Related" },
                new Category { Name = "HouseKeeping" }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("[DB Init] Categories seeded.");
        }

        // Step 4: Seed admin user from environment variables
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@gmail.com";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin@123";

        var existingAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new User
            {
                Username = "System Admin",
                Email = adminEmail,
                DepartmentId = 1,
                Designation = "System Administrator",
                IsActive = true,
                IsApproved = true,
                Role = "ADMIN",
                CreatedAt = DateTime.UtcNow,
                PasswordHash = PasswordUtils.HashPassword(adminPassword)
            };

            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
            logger.LogInformation($"[DB Init] Admin user created: {adminEmail}");
        }
        else
        {
            logger.LogInformation($"[DB Init] Admin user already exists: {adminEmail}");
            if (!PasswordUtils.LooksLikeBcryptHash(existingAdmin.PasswordHash))
            {
                existingAdmin.PasswordHash = PasswordUtils.HashPassword(adminPassword);
                db.Users.Update(existingAdmin);
                await db.SaveChangesAsync();
                logger.LogInformation($"[DB Init] Admin user password hash migrated to BCrypt.");
            }
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError($"[DB Init] Critical error during database initialization: {ex}");
    // Don't fail startup - the app can still serve requests
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception handler — prevents unhandled exceptions from causing empty 500 responses
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
        var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var ex = feature?.Error;
        
        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        
        logger.LogError(ex, "[EXCEPTION] Unhandled exception on path {Path}", feature?.Path);

        var response = new ApiResponse
        {
            Message = "An internal server error occurred.",
            TraceId = ctx.TraceIdentifier,
            Timestamp = DateTime.UtcNow.ToString("o")
        };
        
        // Include details in development only
        if (app.Environment.IsDevelopment())
        {
            response.Exception = ex?.GetType().Name;
            response.StackTrace = ex?.StackTrace;
            response.Path = feature?.Path;
            response.Message = ex?.Message ?? "Unknown error";
        }

        await ctx.Response.WriteAsJsonAsync(response);
    });
});

//app.UseHttpsRedirection();

app.UseMiddleware<TraceIdEnricherMiddleware>();
app.UseSerilogRequestLogging();

//  IMPORTANT ORDER
app.UseStaticFiles();   // serves /uploads/personnel/* photos
app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (AppDbContext db, ILogger<Program> logger) =>
{
    try
    {
        // Check database connectivity
        await db.Database.ExecuteSqlRawAsync("SELECT 1");
        
        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow.ToString("o"),
            database = "connected",
            service = "invmgmt.web"
        });
    }
    catch (Exception ex)
    {
        logger.LogWarning($"[HEALTH] Database check failed: {ex.Message}");
        // Return 503 Service Unavailable if database is down
        return Results.StatusCode(503);
    }
});

app.MapGet("/", (IHostEnvironment env) =>
{
    if (env.IsDevelopment())
    {
        return Results.Redirect("/swagger");
    }

    return Results.Ok(new { service = "invmgmt.web", status = "running" });
});

app.MapControllers();

app.Run();

public partial class Program { }








