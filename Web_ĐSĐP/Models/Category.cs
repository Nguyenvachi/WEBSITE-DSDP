using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Cần cho List và ICollection
using System.ComponentModel.DataAnnotations.Schema; // Cần cho [ForeignKey] nếu bạn muốn chỉ định rõ ràng

namespace E_Sport.Models // Giữ nguyên namespace của bạn
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")] // Tăng độ dài một chút
        [Display(Name = "Tên Danh mục")]
        public string Name { get; set; }

        [DataType(DataType.MultilineText)] // Giúp hiển thị textarea trong một số UI generator
        [Display(Name = "Mô tả Danh mục")]
        public string? Description { get; set; } // THÊM MỚI: Mô tả chi tiết hơn về danh mục

        // Danh sách các sản phẩm thuộc danh mục này (giữ nguyên)
        public List<Product>? Products { get; set; }

        // --- THÊM MỚI: Hỗ trợ danh mục đa cấp (cha-con) ---
        [Display(Name = "Danh mục cha")]
        public int? ParentCategoryId { get; set; } // Nullable, vì danh mục gốc (cấp cao nhất) không có cha

        [ForeignKey("ParentCategoryId")] // Chỉ định khóa ngoại
        public Category? ParentCategory { get; set; } // Navigation property đến danh mục cha

        public ICollection<Category>? SubCategories { get; set; } // Danh sách các danh mục con của danh mục này

        // --- THÊM MỚI: Icon và Hình ảnh cho danh mục (tùy chọn nhưng hữu ích cho UI) ---
        [StringLength(100)]
        [Display(Name = "Icon CSS Class (ví dụ: fas fa-carrot)")]
        public string? IconCssClass { get; set; } // Để lưu class icon (ví dụ từ Font Awesome)

        [StringLength(255)]
        [Display(Name = "URL Hình ảnh đại diện")]
        public string? ImageUrl { get; set; } // URL cho hình ảnh đại diện của danh mục

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0; // Giúp sắp xếp thứ tự hiển thị danh mục

        [Display(Name = "Hiển thị?")]
        public bool IsActive { get; set; } = true; // Cho phép ẩn/hiện danh mục

        // Constructor để khởi tạo các collection
        public Category()
        {
            Products = new List<Product>();
            SubCategories = new HashSet<Category>(); // Dùng HashSet để tránh trùng lặp nếu cần
        }
    }
}
