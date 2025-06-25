using E_Sport.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic; // Cần cho List, IEnumerable
using System.Linq; // Cần cho LINQ methods như Where, Any, Contains
using System.Threading.Tasks; // Cần cho async operations

namespace E_Sport.Repositories
{
    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        public EFProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images) // Giữ lại Include Images nếu bạn có
                .Where(p => p.IsActive) // Chỉ lấy sản phẩm đang được hiển thị
                .ToListAsync();
        }

        // THÊM MỚI: Phương thức GetAllQueryable để HomeController xây dựng truy vấn động
        public IQueryable<Product> GetAllQueryable()
        {
            return _context.Products
                           .Include(p => p.Category)
                           .Include(p => p.Images) // Quan trọng: Include các navigation properties cần thiết
                           .Where(p => p.IsActive); // Nên lọc sản phẩm active ở đây
        }


        public async Task<Product?> GetByIdAsync(int id) // Product có thể null
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive); // Chỉ lấy nếu active
        }

        public async Task AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null) // Kiểm tra null trước khi xóa
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        // Phương thức FilterProductsAsync cũ - Cần được xem xét lại hoặc loại bỏ
        // Tạm thời comment các phần gây lỗi để build được
        public async Task<IEnumerable<Product>> FilterProductsAsync(string? size, string? brand, string? fieldType, decimal? minPrice, decimal? maxPrice)
        {
            var query = _context.Products.Where(p => p.IsActive).AsQueryable();

            // if (!string.IsNullOrEmpty(size))
            //     query = query.Where(p => p.Size == size); // Product.Size không còn

            // if (!string.IsNullOrEmpty(brand))
            //     query = query.Where(p => p.Brand == brand); // Product.Brand không còn

            // if (!string.IsNullOrEmpty(fieldType))
            //     query = query.Where(p => p.FieldType == fieldType); // Product.FieldType không còn

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            return await query.Include(p => p.Category).Include(p => p.Images).ToListAsync();
        }

        // Phương thức FilterAdvancedAsync cũ - Cần được xem xét lại hoặc loại bỏ
        // Tạm thời comment các phần gây lỗi để build được
        public async Task<IEnumerable<Product>> FilterAdvancedAsync(decimal? minPrice, decimal? maxPrice, List<string>? brands)
        {
            var query = _context.Products.Where(p => p.IsActive).AsQueryable();

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // if (brands != null && brands.Any())
            //     query = query.Where(p => p.Brand != null && brands.Contains(p.Brand)); // Product.Brand không còn

            return await query.Include(p => p.Category).Include(p => p.Images).ToListAsync();
        }


        // Phương thức này sẽ được gọi bởi HomeController.AjaxFilter
        // Đổi tên và cập nhật tham số, logic cho Đặc Sản Địa Phương
        public async Task<List<Product>> FilterSpecialtyProductsAsync(
            decimal? minPrice,
            decimal? maxPrice,
            List<int>? categoryIds,
            List<string>? selectedOrigins, // THAY THẾ: selectedSizes bằng selectedOrigins
            List<string>? selectedUnits,   // THÊM MỚI (TÙY CHỌN): selectedUnits
            string? sortBy,                // THÊM MỚI: tham số sắp xếp
            string? searchTerm             // THÊM MỚI: tham số tìm kiếm
            )
        {
            var query = _context.Products
                .Where(p => p.IsActive) // Luôn chỉ lấy sản phẩm đang hoạt động
                .Include(p => p.Category)
                .Include(p => p.Images) // Eager load images
                .AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                string lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(lowerSearchTerm)) ||
                    (p.Description != null && p.Description.ToLower().Contains(lowerSearchTerm)) ||
                    (p.Region != null && p.Region.Name != null && p.Region.Name.ToLower().Contains(lowerSearchTerm)) ||
                    (p.Category != null && p.Category.Name != null && p.Category.Name.ToLower().Contains(lowerSearchTerm))
                );
            }

            // Lọc theo khoảng giá
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Lọc theo danh mục
            if (categoryIds != null && categoryIds.Any())
                query = query.Where(p => categoryIds.Contains(p.CategoryId));

            // Lọc theo Xuất xứ / Vùng miền (THAY THẾ CHO SIZE)
            if (selectedOrigins != null && selectedOrigins.Any())
            {
                query = query.Where(p => p.Region != null && p.Region.Name != null && selectedOrigins.Contains(p.Region.Name));
            }

            // Lọc theo Đơn vị tính (TÙY CHỌN)
            if (selectedUnits != null && selectedUnits.Any())
            {
                query = query.Where(p => p.Unit != null && selectedUnits.Contains(p.Unit));
            }

            // Sắp xếp sản phẩm
            switch (sortBy?.ToLower())
            {
                case "price_asc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                case "name_asc":
                    query = query.OrderBy(p => p.Name);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(p => p.Name);
                    break;
                case "newest":
                    query = query.OrderByDescending(p => p.CreatedDate);
                    break;
                default: // Mặc định hoặc nếu sortBy không hợp lệ
                    query = query.OrderByDescending(p => p.ViewCount).ThenByDescending(p => p.CreatedDate);
                    break;
            }

            return await query.ToListAsync();
        }

        public Product GetById(int id)
        {
            return _context.Products
        .Include(p => p.Category)
        .Include(p => p.Region)
        .Include(p => p.Images)
        .Include(p => p.Reviews)
        .FirstOrDefault(p => p.Id == id && p.IsActive)!;
        }
    }
}
