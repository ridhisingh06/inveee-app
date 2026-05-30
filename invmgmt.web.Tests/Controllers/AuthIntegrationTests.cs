using invmgmt.web.Controllers;
using invmgmt.web.Data;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using invmgmt.web.Repositories;
using invmgmt.web.Services;
using invmgmt.web.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace invmgmt.web.Tests.Controllers;

public class AuthIntegrationTests
{
    private async Task<AppDbContext> GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var db = new AppDbContext(options);
        return db;
    }

    [Fact]
    public async Task UnapprovedLogin_ShouldReturn403_AccountPending()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = await GetDbContext(dbName);

        var pwdHash = PasswordUtils.HashPassword("user@123");
        var user = new User
        {
            Id = 1,
            Username = "Test User",
            Email = "test@example.com",
            IsApproved = false,
            Role = "USER",
            PasswordHash = pwdHash
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns("SUPER_SECRET_KEY_12345678901234567890");
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("issuer");
        configMock.Setup(c => c["Jwt:Audience"]).Returns("audience");

        var userRepo = new UserRepository(db);
        var authService = new AuthService(userRepo, configMock.Object, db, NullLogger<AuthService>.Instance);
        
        var regServiceMock = new Mock<IRegistrationService>();
        var controller = new AuthController(authService, regServiceMock.Object, NullLogger<AuthController>.Instance);

        var loginReq = new LoginRequest { Email = "test@example.com", Password = "user@123" };
        var result = await controller.Login(loginReq);

        var objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objResult.StatusCode);
        
        var msgProp = objResult.Value?.GetType().GetProperty("message")?.GetValue(objResult.Value);
        Assert.Equal("Account pending admin approval", msgProp);
    }

    [Fact]
    public async Task ApprovedUserLogin_ShouldReturn200_WithToken()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = await GetDbContext(dbName);

        var pwdHash = PasswordUtils.HashPassword("user@123");
        var user = new User
        {
            Id = 1,
            Username = "Test User",
            Email = "approved@example.com",
            IsApproved = true,
            Role = "USER",
            PasswordHash = pwdHash
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns("SUPER_SECRET_KEY_12345678901234567890");
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("issuer");
        configMock.Setup(c => c["Jwt:Audience"]).Returns("audience");

        var userRepo = new UserRepository(db);
        var authService = new AuthService(userRepo, configMock.Object, db, NullLogger<AuthService>.Instance);
        var regServiceMock = new Mock<IRegistrationService>();
        var controller = new AuthController(authService, regServiceMock.Object, NullLogger<AuthController>.Instance);

        var loginReq = new LoginRequest { Email = "approved@example.com", Password = "user@123" };
        var result = await controller.Login(loginReq);

        var objResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, objResult.StatusCode);
        
        var tokenProp = objResult.Value?.GetType().GetProperty("token")?.GetValue(objResult.Value);
        Assert.NotNull(tokenProp);
    }

    [Fact]
    public async Task RejectedUserLogin_ShouldReturn403_AccountRejected()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = await GetDbContext(dbName);

        // Rejected user doesn't exist in Users table, only in RegistrationRequests
        var regReq = new RegistrationRequest
        {
            Id = 1,
            Username = "Test User",
            Email = "rejected@example.com",
            Status = RegistrationStatus.Rejected,
            DepartmentId = 1,
            RoleId = 1
        };
        db.RegistrationRequests.Add(regReq);
        await db.SaveChangesAsync();

        var configMock = new Mock<IConfiguration>();
        var userRepo = new UserRepository(db);
        var authService = new AuthService(userRepo, configMock.Object, db, NullLogger<AuthService>.Instance);
        var regServiceMock = new Mock<IRegistrationService>();
        var controller = new AuthController(authService, regServiceMock.Object, NullLogger<AuthController>.Instance);

        var loginReq = new LoginRequest { Email = "rejected@example.com", Password = "user@123" };
        var result = await controller.Login(loginReq);

        var objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objResult.StatusCode); // AuthController maps 'rejected' to 403
        
        var msgProp = objResult.Value?.GetType().GetProperty("message")?.GetValue(objResult.Value);
        Assert.Equal("Account rejected", msgProp);
    }

    [Fact]
    public async Task InvalidPassword_ShouldReturn401_InvalidCredentials()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = await GetDbContext(dbName);

        var pwdHash = PasswordUtils.HashPassword("user@123");
        var user = new User
        {
            Id = 1,
            Username = "Test User",
            Email = "valid@example.com",
            IsApproved = true,
            Role = "USER",
            PasswordHash = pwdHash
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var configMock = new Mock<IConfiguration>();
        var userRepo = new UserRepository(db);
        var authService = new AuthService(userRepo, configMock.Object, db, NullLogger<AuthService>.Instance);
        var regServiceMock = new Mock<IRegistrationService>();
        var controller = new AuthController(authService, regServiceMock.Object, NullLogger<AuthController>.Instance);

        var loginReq = new LoginRequest { Email = "valid@example.com", Password = "wrongpassword" };
        var result = await controller.Login(loginReq);

        var objResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, objResult.StatusCode);
        
        var msgProp = objResult.Value?.GetType().GetProperty("message")?.GetValue(objResult.Value);
        Assert.Equal("Incorrect password", msgProp);
    }
}
