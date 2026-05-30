using System;
using System.Threading.Tasks;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Repositories;
using invmgmt.web.Services;
using invmgmt.web.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace invmgmt.web.Tests.Services
{
    public class RegistrationServiceTests : IDisposable
    {
        private readonly BaseTestFixture _fixture;
        private readonly RegistrationService _service;
        private readonly UserRepository _userRepo;

        public RegistrationServiceTests()
        {
            _fixture = new BaseTestFixture();
            _userRepo = new UserRepository(_fixture.DbContext);
            _service = new RegistrationService(_userRepo, _fixture.DbContext, NullLogger<RegistrationService>.Instance);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task RegisterAsync_ShouldFail_WhenUserAlreadyExists()
        {
            // Arrange
            _fixture.DbContext.Users.Add(new User { Email = "existing@example.com", IsApproved = true });
            await _fixture.DbContext.SaveChangesAsync();

            var dto = new RegistrationRequestDto { Email = "existing@example.com", Username = "Test", Password = "pwd", DepartmentId = 1, RoleId = 1 };

            // Act
            var result = await _service.RegisterAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Email already in use.", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_ShouldFail_WhenDepartmentIsInvalid()
        {
            // Arrange
            var dto = new RegistrationRequestDto { Email = "new@example.com", Username = "Test", Password = "pwd", DepartmentId = 999, RoleId = 1 };

            // Act
            var result = await _service.RegisterAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Department.", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_ShouldSucceed_AndCreatePendingRequest()
        {
            // Arrange
            _fixture.DbContext.Departments.Add(new Department { Id = 1, Name = "IT" });
            await _fixture.DbContext.SaveChangesAsync();

            var dto = new RegistrationRequestDto { Email = "new@example.com", Username = "Test", Password = "pwd", DepartmentId = 1, RoleId = 1, Designation = "Engineer" };

            // Act
            var result = await _service.RegisterAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Your registration is pending. Please wait for admin approval before signing in.", result.Message);

            var user = await _userRepo.GetByEmailAsync("new@example.com");
            Assert.NotNull(user);
            Assert.False(user.IsApproved);
            Assert.Equal("USER", user.Role);

            var regRequest = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_fixture.DbContext.RegistrationRequests, r => r.Email == "new@example.com");
            Assert.NotNull(regRequest);
            Assert.Equal(invmgmt.web.Models.RegistrationStatus.Pending, regRequest.Status);
        }

        [Fact]
        public async Task ApproveAsync_ShouldSucceed_AndSetIsApprovedToTrue()
        {
            // Arrange
            var user = new User { Id = 1, Email = "test@example.com", IsApproved = false };
            _fixture.DbContext.Users.Add(user);
            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var result = await _service.ApproveAsync(1);

            // Assert
            Assert.True(result.Success);
            var updatedUser = await _userRepo.GetByIdAsync(1);
            Assert.True(updatedUser?.IsApproved);
        }

        [Fact]
        public async Task RejectAsync_ShouldRemoveUser()
        {
            // Arrange
            var user = new User { Id = 1, Email = "test@example.com", IsApproved = false };
            _fixture.DbContext.Users.Add(user);
            await _fixture.DbContext.SaveChangesAsync();

            // Act
            var result = await _service.RejectAsync(1);

            // Assert
            Assert.True(result.Success);
            var deletedUser = await _userRepo.GetByIdAsync(1);
            Assert.Null(deletedUser);
        }
    }
}
