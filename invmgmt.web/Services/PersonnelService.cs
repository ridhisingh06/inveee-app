using invmgmt.web.DTOs;
using invmgmt.web.Models;
using invmgmt.web.Repositories;

namespace invmgmt.web.Services
{
    public class PersonnelService : IPersonnelService
    {
        private readonly IPersonnelRepository _repo;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PersonnelService> _logger;

        private const string UploadFolder = "uploads/personnel";

        public PersonnelService(
            IPersonnelRepository repo,
            IWebHostEnvironment env,
            ILogger<PersonnelService> logger)
        {
            _repo = repo;
            _env = env;
            _logger = logger;
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<PersonnelResponseDto> CreateAsync(PersonnelCreateDto dto, IFormFile? photo, string baseUrl)
        {
            // Duplicate email check
            if (await _repo.EmailExistsAsync(dto.Email))
                throw new InvalidOperationException($"A personnel record with email '{dto.Email}' already exists.");

            var personnel = MapToEntity(dto);
            personnel.CreatedAt = DateTime.UtcNow;

            if (photo != null)
                personnel.PhotoPath = await SavePhotoAsync(photo);

            await _repo.AddAsync(personnel);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("Personnel created: {Name} (id={Id})", personnel.Name, personnel.Id);
            return MapToDto(personnel, baseUrl);
        }

        // ── GET ALL (paginated) ───────────────────────────────────────────────
        public async Task<PersonnelPagedResultDto> GetAllAsync(int page, int pageSize, string baseUrl)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (items, total) = await _repo.GetAllAsync(page, pageSize);
            return new PersonnelPagedResultDto
            {
                Data = items.Select(p => MapToDto(p, baseUrl)),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────
        public async Task<PersonnelResponseDto?> GetByIdAsync(int id, string baseUrl)
        {
            var p = await _repo.GetByIdAsync(id);
            return p == null ? null : MapToDto(p, baseUrl);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<PersonnelResponseDto> UpdateAsync(int id, PersonnelCreateDto dto, IFormFile? photo, string baseUrl)
        {
            var personnel = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Personnel with id {id} not found.");

            if (await _repo.EmailExistsAsync(dto.Email, excludeId: id))
                throw new InvalidOperationException($"Email '{dto.Email}' is already in use by another record.");

            // Update fields
            personnel.Name = dto.Name;
            personnel.ICNumber = dto.ICNumber;
            personnel.BirthDate = dto.BirthDate;
            personnel.Email = dto.Email;
            personnel.ResidentialAddress = dto.ResidentialAddress;
            personnel.ResidentialPhone = dto.ResidentialPhone;
            personnel.OfficePhone = dto.OfficePhone;
            personnel.Designation = dto.Designation;
            personnel.JobDescription = dto.JobDescription;
            personnel.Department = dto.Department;
            personnel.IsStoresIncharge = dto.IsStoresIncharge;
            personnel.Building = dto.Building;
            personnel.ReportingOfficer = dto.ReportingOfficer;
            personnel.IdCardNumber = dto.IdCardNumber;
            personnel.IdCardExpiryDate = dto.IdCardExpiryDate;
            personnel.UpdatedAt = DateTime.UtcNow;

            if (photo != null)
            {
                DeletePhoto(personnel.PhotoPath);
                personnel.PhotoPath = await SavePhotoAsync(photo);
            }

            await _repo.UpdateAsync(personnel);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("Personnel updated: id={Id}", id);
            return MapToDto(personnel, baseUrl);
        }

        // ── DELETE ────────────────────────────────────────────────────────────
        public async Task DeleteAsync(int id)
        {
            var personnel = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Personnel with id {id} not found.");

            DeletePhoto(personnel.PhotoPath);
            await _repo.DeleteAsync(personnel);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("Personnel deleted: id={Id}", id);
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private async Task<string> SavePhotoAsync(IFormFile file)
        {
            // Validate
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not ".jpg" and not ".jpeg")
                throw new ArgumentException("Only JPG/JPEG files are allowed.");
            if (file.Length > 2 * 1024 * 1024)
                throw new ArgumentException("Photo must be under 2 MB.");

            var dir = Path.Combine(_env.WebRootPath, UploadFolder);
            Directory.CreateDirectory(dir);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(dir, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"{UploadFolder}/{fileName}";   // relative — stored in DB
        }

        private void DeletePhoto(string? photoPath)
        {
            if (string.IsNullOrWhiteSpace(photoPath)) return;
            var full = Path.Combine(_env.WebRootPath, photoPath.TrimStart('/'));
            if (File.Exists(full))
            {
                File.Delete(full);
                _logger.LogInformation("Deleted photo file: {Path}", full);
            }
        }

        private static Personnel MapToEntity(PersonnelCreateDto dto) => new()
        {
            Name = dto.Name,
            ICNumber = dto.ICNumber,
            BirthDate = dto.BirthDate,
            Email = dto.Email,
            ResidentialAddress = dto.ResidentialAddress,
            ResidentialPhone = dto.ResidentialPhone,
            OfficePhone = dto.OfficePhone,
            Designation = dto.Designation,
            JobDescription = dto.JobDescription,
            Department = dto.Department,
            IsStoresIncharge = dto.IsStoresIncharge,
            Building = dto.Building,
            ReportingOfficer = dto.ReportingOfficer,
            IdCardNumber = dto.IdCardNumber,
            IdCardExpiryDate = dto.IdCardExpiryDate
        };

        private static PersonnelResponseDto MapToDto(Personnel p, string baseUrl) => new()
        {
            Id = p.Id,
            Name = p.Name,
            ICNumber = p.ICNumber,
            BirthDate = p.BirthDate,
            Email = p.Email,
            ResidentialAddress = p.ResidentialAddress,
            ResidentialPhone = p.ResidentialPhone,
            OfficePhone = p.OfficePhone,
            Designation = p.Designation,
            JobDescription = p.JobDescription,
            Department = p.Department,
            IsStoresIncharge = p.IsStoresIncharge,
            Building = p.Building,
            ReportingOfficer = p.ReportingOfficer,
            IdCardNumber = p.IdCardNumber,
            IdCardExpiryDate = p.IdCardExpiryDate,
            PhotoUrl = string.IsNullOrEmpty(p.PhotoPath)
                ? null
                : $"{baseUrl.TrimEnd('/')}/{p.PhotoPath}",
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
