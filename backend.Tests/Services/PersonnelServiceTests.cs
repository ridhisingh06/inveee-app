using System;
using System.IO;
using System.Threading.Tasks;
using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Repositories;
using invmgmt.web.Services;
using invmgmt.web.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace invmgmt.web.Tests.Services
{
    public class PersonnelServiceTests : IDisposable
    {
        private readonly BaseTestFixture _fixture;
        private readonly PersonnelService _service;
        private readonly PersonnelRepository _repo;
        private readonly Mock<IWebHostEnvironment> _envMock;

        public PersonnelServiceTests()
        {
            _fixture = new BaseTestFixture();
            _repo = new PersonnelRepository(_fixture.DbContext);
            
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            _service = new PersonnelService(_repo, _envMock.Object, NullLogger<PersonnelService>.Instance);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenEmailExists()
        {
            // Arrange
            _fixture.DbContext.Personnel.Add(new Personnel { Email = "existing@example.com", Name = "Test" });
            await _fixture.DbContext.SaveChangesAsync();

            var dto = new PersonnelCreateDto { Email = "existing@example.com", Name = "New" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto, null, "http://localhost"));
        }

        [Fact]
        public async Task CreateAsync_ShouldSucceed_AndSaveToDb()
        {
            // Arrange
            var dto = new PersonnelCreateDto { Email = "new@example.com", Name = "John Doe" };

            // Act
            var result = await _service.CreateAsync(dto, null, "http://localhost");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John Doe", result.Name);
            
            var saved = await _repo.GetByIdAsync(result.Id);
            Assert.NotNull(saved);
            Assert.Equal("new@example.com", saved.Email);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrow_WhenPersonnelNotFound()
        {
            // Arrange
            var dto = new PersonnelCreateDto { Email = "update@example.com", Name = "Updated" };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(999, dto, null, "http://localhost"));
        }

        [Fact]
        public async Task UpdateAsync_ShouldSucceed()
        {
            // Arrange
            var personnel = new Personnel { Email = "test@example.com", Name = "Old Name" };
            _fixture.DbContext.Personnel.Add(personnel);
            await _fixture.DbContext.SaveChangesAsync();

            var dto = new PersonnelCreateDto { Email = "test@example.com", Name = "New Name" };

            // Act
            var result = await _service.UpdateAsync(personnel.Id, dto, null, "http://localhost");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.Name);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveFromDb()
        {
            // Arrange
            var personnel = new Personnel { Email = "delete@example.com", Name = "To Delete" };
            _fixture.DbContext.Personnel.Add(personnel);
            await _fixture.DbContext.SaveChangesAsync();

            // Act
            await _service.DeleteAsync(personnel.Id);

            // Assert
            var deleted = await _repo.GetByIdAsync(personnel.Id);
            Assert.Null(deleted);
        }
    }
}
