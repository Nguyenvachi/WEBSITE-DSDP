using System.Collections.Generic;
using E_Sport.Models;

namespace E_Sport.Repositories
{
    public interface IReviewRepository
    {
        IEnumerable<Review> GetByProduct(int productId);
        void Add(Review review);
        void SaveChanges();
    }
}
