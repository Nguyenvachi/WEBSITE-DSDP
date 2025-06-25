using E_Sport.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E_Sport.Repositories
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllAsync();
        Task<Order> GetByIdAsync(int id);
        Task AddAsync(Order order);
        Task DeleteAsync(int id);
        Task UpdateStatusAsync(int orderId, string newStatus);

    }
}
