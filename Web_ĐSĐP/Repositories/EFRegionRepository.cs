using E_Sport.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E_Sport.Repositories
{
    public class EFRegionRepository : IRegionRepository
    {
        private readonly ApplicationDbContext _context;

        public EFRegionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Region region)
        {
            _context.Regions.Add(region);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var region = await _context.Regions.FindAsync(id);
            if (region != null)
            {
                _context.Regions.Remove(region);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Region>> GetAllAsync()
        {
            // Lấy tất cả các vùng miền, sắp xếp theo tên
            return await _context.Regions.OrderBy(r => r.Name).ToListAsync();
        }

        public async Task<Region?> GetByIdAsync(int id)
        {
            // Lấy một vùng miền theo ID, có thể trả về null nếu không tìm thấy
            return await _context.Regions.FindAsync(id);
        }

        public async Task UpdateAsync(Region region)
        {
            _context.Regions.Update(region);
            await _context.SaveChangesAsync();
        }
    }
}
