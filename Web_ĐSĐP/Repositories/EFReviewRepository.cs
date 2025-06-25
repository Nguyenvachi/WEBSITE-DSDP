using System.Collections.Generic;
using System.Linq;
using E_Sport.Models;

namespace E_Sport.Repositories
{
    public class EFReviewRepository : IReviewRepository
    {
        private readonly ApplicationDbContext _context;
        public EFReviewRepository(ApplicationDbContext context) => _context = context;

        public IEnumerable<Review> GetByProduct(int productId) =>
            _context.Reviews
                    .Where(r => r.ProductId == productId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList();

        public void Add(Review review) => _context.Reviews.Add(review);
        public void SaveChanges() => _context.SaveChanges();
    }
}
