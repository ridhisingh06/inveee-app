using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace invmgmt.web.Repositories
{
    public interface IRequestRepository
    {
        Task<Request?> GetByIdAsync(int id);
        Task<Request?> GetByIdWithItemsAsync(int id);
        Task<IEnumerable<Request>> GetUserRequestsAsync(int userId, int pageNumber, int pageSize);
        Task<IEnumerable<Request>> GetPendingRequestsAsync(int pageNumber, int pageSize);
        Task<bool> HasActiveRequestAsync(int userId);
        Task AddRequestAsync(Request request);
        Task AddRequestItemsAsync(IEnumerable<RequestItem> items);
        Task UpdateRequestAsync(Request request);
        Task<bool> ItemsExistAsync(IEnumerable<int> itemIds);
        void DeleteRequest(Request request);
        Task SaveChangesAsync();
    }
}
