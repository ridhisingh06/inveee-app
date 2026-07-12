using invmgmt.web.Data;
using invmgmt.web.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace invmgmt.web.Services
{
    public class SectionWiseQueryService : ISectionWiseQueryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SectionWiseQueryService> _logger;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public SectionWiseQueryService(AppDbContext context, ILogger<SectionWiseQueryService> logger, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<List<SectionWiseQueryOfficerDto>> GetOfficersAsync()
        {
            // Cache officers for a short period to reduce DB load
            return await _cache.GetOrCreateAsync("SectionWise_Officers", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return await _context.Personnel
                    .AsNoTracking()
                    .Where(p => !string.IsNullOrEmpty(p.Name))
                    .OrderBy(p => p.Name)
                    .Select(p => new SectionWiseQueryOfficerDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Building = p.Building
                    })
                    .ToListAsync();
            });
        }

        public async Task<List<string>> GetBhawansAsync()
        {
            return await _cache.GetOrCreateAsync("SectionWise_Bhawans", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return await _context.Personnel
                    .AsNoTracking()
                    .Where(p => !string.IsNullOrEmpty(p.Building))
                    .Select(p => p.Building!)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToListAsync();
            });
        }

        public async Task<List<SectionWiseQueryItemDto>> SearchItemsAsync(string query)
        {
            var q = (query ?? string.Empty).Trim().ToLower();
            return await _context.Items
                .AsNoTracking()
                .Include(i => i.Category)
                .Include(i => i.InventoryStock)
                .Where(i => i.IsActive &&
                    (string.IsNullOrEmpty(q)
                        || (i.Name != null && i.Name.ToLower().Contains(q))
                        || (i.Description != null && i.Description.ToLower().Contains(q))))
                .OrderBy(i => i.Name)
                .Take(50)
                .Select(i => new SectionWiseQueryItemDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Category = i.Category != null ? i.Category.Name : string.Empty,
                    AvailableQuantity = i.InventoryStock != null ? i.InventoryStock.AvailableQuantity : 0
                })
                .ToListAsync();
        }

        public async Task<SectionWiseQueryResultDto> GetSectionWiseQueryAsync(SectionWiseQueryFilterDto filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate > filter.ToDate)
                throw new ArgumentException("From Date cannot be later than To Date.");

            var query = _context.RequestItems
                .AsNoTracking()
                .Include(ri => ri.Request)
                    .ThenInclude(r => r.User)
                .Include(ri => ri.Item)
                .Where(ri => ri.Request != null && ri.Request.User != null);

            if (filter.FromDate.HasValue)
            {
                var from = filter.FromDate.Value.Date;
                query = query.Where(ri => ri.Request.CreatedAt >= from);
            }

            if (filter.ToDate.HasValue)
            {
                var to = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(ri => ri.Request.CreatedAt <= to);
            }

            if (filter.OfficerId.HasValue)
            {
                var officer = await _context.Personnel
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == filter.OfficerId.Value);

                if (officer != null)
                {
                    var normalizedEmail = officer.Email?.Trim().ToLower() ?? string.Empty;
                    var normalizedName = officer.Name.Trim().ToLower();

                    query = query.Where(ri => ri.Request.User.Email.ToLower() == normalizedEmail
                                              || ri.Request.User.Username.ToLower() == normalizedName);
                }
                else
                {
                    query = query.Where(_ => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Bhawan))
            {
                var searchBhawan = filter.Bhawan.Trim().ToLower();
                var emails = await _context.Personnel
                    .AsNoTracking()
                    .Where(p => p.Building != null && p.Building.ToLower() == searchBhawan)
                    .Select(p => p.Email.ToLower())
                    .Distinct()
                    .ToListAsync();

                if (emails.Count == 0)
                {
                    query = query.Where(_ => false);
                }
                else
                {
                    query = query.Where(ri => emails.Contains(ri.Request.User.Email.ToLower()));
                }
            }

            if (filter.ItemId.HasValue)
            {
                query = query.Where(ri => ri.ItemId == filter.ItemId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ItemName))
            {
                var itemName = filter.ItemName.Trim().ToLower();
                query = query.Where(ri => ri.Item.Name != null && ri.Item.Name.ToLower().Contains(itemName));
            }

            filter.PageNumber = Math.Max(1, filter.PageNumber);
            filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

            var totalCount = await query.CountAsync();
            var rawRows = await query
                .OrderByDescending(ri => ri.Request.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(ri => new
                {
                    ri.Id,
                    ri.RequestId,
                    OfficerName = ri.Request.User.Username,
                    RequestUserEmail = ri.Request.User.Email,
                    ri.ItemId,
                    ItemName = ri.Item != null ? ri.Item.Name : string.Empty,
                    ri.QuantityRequested,
                    ri.QuantityApproved,
                    ri.QuantityIssued,
                    RequestStatus = ri.Request.Status,
                    RequestDate = ri.Request.CreatedAt,
                    RequestedBy = ri.Request.User.Username
                })
                .ToListAsync();

            var requestEmails = rawRows
                .Select(r => (r.RequestUserEmail ?? string.Empty).ToLower())
                .Distinct()
                .Where(email => !string.IsNullOrEmpty(email))
                .ToList();

            var matchingPersonnel = await _context.Personnel
                .AsNoTracking()
                .Where(p => requestEmails.Contains(p.Email.ToLower()))
                .Select(p => new { Email = p.Email.ToLower(), p.Building })
                .ToListAsync();

            var bhawanLookup = matchingPersonnel
                .GroupBy(p => p.Email)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Building).FirstOrDefault());

            var rows = rawRows.Select(r => new SectionWiseQueryRowDto
            {
                RequestItemId = r.Id,
                RequestId = r.RequestId,
                OfficerName = r.OfficerName ?? string.Empty,
                Bhawan = !string.IsNullOrEmpty(r.RequestUserEmail) && bhawanLookup.TryGetValue(r.RequestUserEmail.ToLower(), out var bhawan)
                    ? bhawan
                    : null,
                ItemId = r.ItemId,
                ItemName = r.ItemName,
                QuantityRequested = r.QuantityRequested,
                QuantityApproved = r.QuantityApproved,
                QuantityIssued = r.QuantityIssued,
                RequestStatus = r.RequestStatus.ToString(),
                RequestDate = r.RequestDate,
                RequestedBy = r.RequestedBy ?? string.Empty
            }).ToList();

            return new SectionWiseQueryResultDto
            {
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount,
                Data = rows
            };
        }
    }
}
