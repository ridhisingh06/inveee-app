using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// =======================
// 🔥 SERVICES REGISTER
// =======================

// Controllers
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();


// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔐 JWT AUTHENTICATION
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// =======================
// BUILD APP
// =======================
var app = builder.Build();



// =======================
// MIDDLEWARE PIPELINE
// =======================

app.UseAuthentication();   // 🔐 check login token
app.UseAuthorization();    // 🚫 allow/deny access

app.MapControllers();

app.Run();