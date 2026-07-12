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
// Kestrel – listen on every interface, HTTP port 5000
// ---------------------------------------------------------------
builder.WebHost.ConfigureKestrel(options =>
{
    // Bind to 0.0.0.0:5000  (accessible from any NIC)
    options.ListenAnyIP(5000);
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
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<IRequestItemRepository, RequestItemRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IOrderSummaryRepository, OrderSummaryRepository>();
builder.Services.AddScoped<invmgmt.web.Repositories.IPersonnelRepository, invmgmt.web.Repositories.PersonnelRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IIssuerService, IssuerService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IOrderSummaryService, OrderSummaryService>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<ISectionWiseQueryService, SectionWiseQueryService>();
builder.Services.AddScoped<invmgmt.web.Services.IPersonnelService, invmgmt.web.Services.PersonnelService>();

// Memory cache for lightweight server-side caching in production
builder.Services.AddMemoryCache();
// =======================
// CORS (PRODUCTION CONFIGURED)
// =======================
builder.Services.AddCors(options =>
{
    // ── Stable production origin (custom domain — never changes) ─────────────
    // https://inveee-app.vercel.app is the project's permanent Vercel URL.
    // It is aliased to every deployment, so no slug churn, no config updates
    // needed when Vercel creates a new preview build.
    //
    // Additional origins can still be supplied at runtime via FRONTEND_URL
    // (comma-separated) for local overrides or staging environments.
    var hardcodedOrigins = new[]
    {
        "https://inveee-app.vercel.app", // stable production domain — never changes
        "http://localhost:4200",         // Angular dev server
        "http://localhost:3000",         // alternative dev port
        "https://localhost:4200",        // HTTPS dev
    };

    // ── Runtime origins from FRONTEND_URL env var ────────────────────────────
    // Optional: comma-separated extra origins injected via ECS task-definition.
    // The hardcoded set above is always present regardless of this value.
    var rawFrontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? string.Empty;

    var allowedOrigins = rawFrontendUrl
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Concat(hardcodedOrigins)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    Console.WriteLine($"[STARTUP] Allowed CORS Origins ({allowedOrigins.Length}):");
    foreach (var origin in allowedOrigins)
        Console.WriteLine($"[STARTUP]   -> {origin}");

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()      // GET, POST, PUT, DELETE, OPTIONS, PATCH
              .AllowAnyHeader()      // Content-Type, Authorization, etc.
              .AllowCredentials();   // Required for Authorization header / cookies
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
        bool dbConnected = false;
        
        // STEP 0: Test database connection first
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                logger.LogInformation($"[DB Init] Attempting database connection (attempt {i + 1}/{maxRetries})...");
                await db.Database.ExecuteSqlRawAsync("SELECT 1");
                dbConnected = true;
                logger.LogInformation("[DB Init] ✓ Database connection successful!");
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[DB Init] Connection failed: {ex.GetType().Name}: {ex.Message}");
                if (i < maxRetries - 1)
                {
                    logger.LogInformation($"[DB Init] Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, 10000);
                }
                else
                {
                    logger.LogError($"[DB Init] ✗ FAILED to connect after {maxRetries} attempts!");
                }
            }
        }

        if (!dbConnected)
        {
            throw new InvalidOperationException("Database connection failed after multiple retries. Check connection string and RDS availability.");
        }

        // STEP 1: Apply migrations
        logger.LogInformation("[DB Init] Checking for pending migrations...");
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation($"[DB Init] Found {pendingMigrations.Count()} pending migrations. Applying...");
            await db.Database.MigrateAsync();
            logger.LogInformation("[DB Init] ✓ Migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("[DB Init] No pending migrations.");
        }

        // STEP 2: Seed Roles
        if (!await db.Roles.AnyAsync())
        {
            logger.LogInformation("[DB Init] Seeding roles...");
            db.Roles.AddRange(
                new Role { Id = 1, Name = "User" },
                new Role { Id = 2, Name = "Issuer" },
                new Role { Id = 3, Name = "Admin" }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("[DB Init] Roles seeded.");
        }

        // STEP 3: Seed Departments
        if (!await db.Departments.AnyAsync())
        {
            logger.LogInformation("[DB Init] Seeding departments...");
            db.Departments.AddRange(
                new Department { Id = 1, Name = "Admin" },
                new Department { Id = 2, Name = "IT" },
                new Department { Id = 3, Name = "HR" },
                new Department { Id = 4, Name = "Finance" }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("[DB Init] ✓ Departments seeded.");
        }

        // STEP 4: Seed Categories
        if (!await db.Categories.AnyAsync())
        {
            logger.LogInformation("[DB Init] Seeding categories...");
            db.Categories.AddRange(
                new Category { Name = "Stationary" },
                new Category { Name = "IT Related" },
                new Category { Name = "HouseKeeping" }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("[DB Init] ✓ Categories seeded.");
        }

        // STEP 5: CRITICAL - Seed admin user
        logger.LogInformation("[DB Init] ===== CRITICAL: Checking admin user =====");
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@gmail.com";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin@123";

        logger.LogInformation($"[DB Init] Expected admin email: {adminEmail}");

        var existingAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        
        if (existingAdmin == null)
        {
            logger.LogWarning($"[DB Init] ⚠ Admin user NOT found in database! Creating new admin user...");
            
            var hashedPassword = PasswordUtils.HashPassword(adminPassword);
            logger.LogInformation($"[DB Init] Generated password hash (first 30 chars): {hashedPassword.Substring(0, Math.Min(30, hashedPassword.Length))}...");
            logger.LogInformation($"[DB Init] FULL HASH FOR VERIFICATION: {hashedPassword}");
            
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
                PasswordHash = hashedPassword
            };

            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
            logger.LogInformation($"[DB Init] ✓ ADMIN USER CREATED: ID={adminUser.Id}, Email={adminEmail}");
            
            // Verify the hash was saved correctly
            var verifyAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (verifyAdmin != null)
            {
                logger.LogInformation($"[DB Init] ✓ VERIFICATION: Admin user persisted to database");
                logger.LogInformation($"[DB Init] Stored hash (first 30): {verifyAdmin.PasswordHash.Substring(0, Math.Min(30, verifyAdmin.PasswordHash.Length))}...");
                logger.LogInformation($"[DB Init] FULL STORED HASH: {verifyAdmin.PasswordHash}");
                
                // Test password verification immediately
                bool hashWorks = PasswordUtils.VerifyPassword(adminPassword, verifyAdmin.PasswordHash);
                logger.LogInformation($"[DB Init] Password verification test: {(hashWorks ? "✓ PASS" : "✗ FAIL")}");
            }
        }
        else
        {
            logger.LogInformation($"[DB Init] ✓ Admin user found: ID={existingAdmin.Id}, Email={existingAdmin.Email}");
            logger.LogInformation($"[DB Init]   IsApproved={existingAdmin.IsApproved}, IsActive={existingAdmin.IsActive}, Role={existingAdmin.Role}");
            logger.LogInformation($"[DB Init]   Password hash starts with: {existingAdmin.PasswordHash.Substring(0, Math.Min(20, existingAdmin.PasswordHash.Length))}...");
            logger.LogInformation($"[DB Init]   FULL HASH: {existingAdmin.PasswordHash}");
            
            // Test password verification
            bool hashMatches = PasswordUtils.VerifyPassword(adminPassword, existingAdmin.PasswordHash);
            logger.LogInformation($"[DB Init] Password verification test: {(hashMatches ? "✓ PASS" : "✗ FAIL")}");
            
            // Verify password hash is valid BCrypt
            if (!PasswordUtils.LooksLikeBcryptHash(existingAdmin.PasswordHash))
            {
                logger.LogWarning($"[DB Init] ⚠ Admin password hash is NOT valid BCrypt format! Updating...");
                var newHash = PasswordUtils.HashPassword(adminPassword);
                existingAdmin.PasswordHash = newHash;
                db.Users.Update(existingAdmin);
                await db.SaveChangesAsync();
                logger.LogInformation($"[DB Init] ✓ Admin password hash updated to BCrypt.");
                logger.LogInformation($"[DB Init] New hash: {newHash}");
            }
            else
            {
                logger.LogInformation($"[DB Init] ✓ Admin password hash is valid BCrypt format.");
            }
        }

        logger.LogInformation("[DB Init] ===== Database initialization COMPLETE =====");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "[DB Init] ✗ ===== CRITICAL DATABASE INITIALIZATION ERROR =====");
    logger.LogError("[DB Init] Exception Type: {ExceptionType}", ex.GetType().FullName);
    logger.LogError("[DB Init] Exception Message: {Message}", ex.Message);
    logger.LogError("[DB Init] Stack Trace: {StackTrace}", ex.StackTrace);
    
    if (ex.InnerException != null)
    {
        logger.LogError("[DB Init] Inner Exception: {InnerMessage}", ex.InnerException.Message);
    }
    
    // FAIL FAST: Don't start the app if database init fails
    throw;
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ---------------------------------------------------------------
// MIDDLEWARE PIPELINE — ORDER IS CRITICAL FOR CORS + PREFLIGHT
//
// ASP.NET Core requires UseCors() to sit BETWEEN UseRouting() and
// UseAuthorization() so it can read endpoint-level CORS metadata
// (set via RequireCors / [EnableCors]) that routing resolves first.
//
// Placing UseCors before UseRouting causes:
//   "Endpoint contains CORS metadata, but a middleware was not found
//    that supports CORS." → HTTP 500
// ---------------------------------------------------------------

// 1. Global exception handler — registered first so it wraps everything
//    downstream. CORS headers must be written explicitly here because
//    UseCors() sits later in the pipeline and its headers are not yet
//    present when this handler runs. Without them the browser treats a
//    500 as a network error and never surfaces the actual status code.
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
        var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var ex = feature?.Error;

        // ── Emit CORS headers on error responses ────────────────────────────
        // The browser requires Access-Control-Allow-Origin on every response,
        // including 5xx errors. We copy the Origin echo pattern that UseCors
        // would normally apply.
        var origin = ctx.Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrEmpty(origin))
        {
            ctx.Response.Headers["Access-Control-Allow-Origin"] = origin;
            ctx.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            ctx.Response.Headers["Vary"] = "Origin";
        }

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

// 2. HTTPS redirection disabled — API sits behind CloudFront/ALB which
//    handles TLS termination and forwards plain HTTP to ECS on port 5000.
//app.UseHttpsRedirection();

// 3. Diagnostics / logging
app.UseMiddleware<TraceIdEnricherMiddleware>();
app.UseSerilogRequestLogging();

// 4. Static files — before routing so file requests short-circuit cleanly.
app.UseStaticFiles();   // serves /uploads/personnel/* photos

// 5. Routing — resolves endpoint metadata (including CORS policy names).
//    UseCors MUST come after this.
app.UseRouting();

// 6. CORS — after UseRouting so endpoint CORS metadata is available;
//    before UseAuthentication so OPTIONS preflight is never blocked by auth.
app.UseCors("AllowFrontend");

// 7. Auth
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () =>
{
    // Lightweight health check for ALB, no DB call
    return Results.Ok(new
    {
        status = "ok",
        service = "invmgmt.web"
    });
});

app.MapGet("/health/db", async (AppDbContext db, ILogger<Program> logger) =>
{
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1");
        return Results.Ok(new { status = "db ok" });
    }
    catch (Exception ex)
    {
        logger.LogWarning($"[HEALTH] Database check failed: {ex.Message}");
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

app.MapControllers().RequireCors("AllowFrontend");

app.Run();

public partial class Program { }








