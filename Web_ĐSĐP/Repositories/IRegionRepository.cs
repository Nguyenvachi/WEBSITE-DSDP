using E_Sport.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E_Sport.Repositories
{
    public interface IRegionRepository
    {
        Task<IEnumerable<Region>> GetAllAsync();
        Task<Region?> GetByIdAsync(int id);
        Task AddAsync(Region region);
        Task UpdateAsync(Region region);
        Task DeleteAsync(int id);
    }
}
