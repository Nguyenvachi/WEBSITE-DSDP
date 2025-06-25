using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace E_Sport.Models // Giữ nguyên namespace của bạn
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Review> Reviews { get; set; }

        // THÊM MỚI: DbSet cho Region
        public DbSet<Region> Regions { get; set; }

        // THÊM MỚI hoặc CẬP NHẬT: Phương thức OnModelCreating để cấu hình Fluent API
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Quan trọng: Gọi base.OnModelCreating đầu tiên

            // 1. Cấu hình kiểu dữ liệu cho Price của Product (đảm bảo độ chính xác)
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // 2. Cấu hình mối quan hệ tự tham chiếu cho Category (danh mục cha-con)
            // Một Category (ParentCategory) có thể có nhiều SubCategories
            // Một Category (SubCategory) chỉ có một ParentCategory (hoặc null nếu là danh mục gốc)
            builder.Entity<Category>()
                .HasOne(c => c.ParentCategory)      // Một Category có một ParentCategory (hoặc null)
                .WithMany(p => p.SubCategories)     // ParentCategory đó có nhiều SubCategories
                .HasForeignKey(c => c.ParentCategoryId) // Khóa ngoại là ParentCategoryId
                .OnDelete(DeleteBehavior.Restrict); // Hoặc .SetNull: Khi xóa danh mục cha,
                                                    // ParentCategoryId của các danh mục con sẽ được set thành null.
                                                    // DeleteBehavior.Restrict sẽ ngăn việc xóa nếu còn danh mục con.
                                                    // Chọn Restrict để an toàn hơn, tránh mất dữ liệu không mong muốn.

            // 3. Cấu hình mối quan hệ giữa Product và Region
            // Một Region có thể có nhiều Product
            // Một Product thuộc về một Region (hoặc null, vì RegionId trong Product là nullable)
            builder.Entity<Product>()
                .HasOne(p => p.Region)              // Một Product có một Region (hoặc null)
                .WithMany(r => r.Products)          // Region đó có nhiều Products
                .HasForeignKey(p => p.RegionId)     // Khóa ngoại là RegionId trong Product
                .IsRequired(false)                  // false vì RegionId là nullable (int?)
                .OnDelete(DeleteBehavior.SetNull);  // Khi một Region bị xóa, RegionId trong các Product liên quan sẽ được set thành NULL.
                                                    // Điều này cho phép sản phẩm vẫn tồn tại mà không thuộc về vùng miền nào nữa.
                                                    // Nếu muốn khi xóa Region thì xóa luôn sản phẩm, dùng .OnDelete(DeleteBehavior.Cascade) - CẨN THẬN.

            // 4. (Nếu có) Cấu hình mối quan hệ giữa Product và ProductImage (một Product có nhiều ProductImage)
            // Nếu bạn chưa có cấu hình này và ProductImage có ProductId
            builder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Khi xóa Product, các ProductImage liên quan cũng sẽ bị xóa.

            // Thêm các cấu hình Fluent API khác nếu cần cho các entities còn lại
            // ví dụ: Order, OrderDetail, Favorite...
        }
    }
}
