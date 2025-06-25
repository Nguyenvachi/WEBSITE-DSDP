// File: Repositories/IProductRepository.cs
using E_Sport.Models;
using System.Collections.Generic;
using System.Linq; // Cần cho IQueryable
using System.Threading.Tasks;

namespace E_Sport.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        IQueryable<Product> GetAllQueryable(); // Đã đúng từ lần trước
        Task<Product?> GetByIdAsync(int id);   // SỬA: Kiểu trả về là Product? (nullable)
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
        Product GetById(int id);

        // SỬA ĐỔI HOÀN TOÀN CHỮ KÝ CHO PHÙ HỢP
        Task<List<Product>> FilterSpecialtyProductsAsync( // SỬA: Kiểu trả về Task<List<Product>>
            decimal? minPrice,
            decimal? maxPrice,
            List<int>? categoryIds,       // SỬA: Thêm nullable List<int>?
            List<string>? selectedOrigins, // SỬA: Thêm nullable List<string>?
            List<string>? selectedUnits,   // SỬA: Thêm nullable List<string>?
            string? sortBy,
            string? searchTerm
        );

        // --- XEM XÉT XÓA HOẶC COMMENT CÁC PHƯƠNG THỨC LỌC CŨ NÀY ---
        // --- NẾU CHÚNG KHÔNG CÒN ĐƯỢC SỬ DỤNG HOẶC GÂY LỖI IMPLEMENT ---
        /*
        Task<IEnumerable<Product>> FilterProductsAsync(
            string? size, // Nên là nullable nếu có thể không truyền
            string? brand, 
            string? fieldType, 
            decimal? minPrice, 
            decimal? maxPrice
        );

        Task<IEnumerable<Product>> FilterAdvancedAsync(
            decimal? minPrice, 
            decimal? maxPrice, 
            List<string>? brands // Nên là nullable
        );

        Task<IEnumerable<Product>> FilterByCategoryAndPriceAsync(
            decimal? minPrice, 
            decimal? maxPrice, 
            List<int>? categoryIds // Nên là nullable
        );

        Task<List<Product>> FilterByCategoryPriceSizeAsync( // Phương thức này đã được thay thế bằng FilterSpecialtyProductsAsync
            decimal? minPrice, 
            decimal? maxPrice, 
            List<int>? categoryIds, // Nên là nullable
            List<string>? selectedSizes // Nên là nullable
        );
        */
    }
}
