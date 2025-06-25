// File: Models/ViewModels/ProductFilterViewModel.cs
using E_Sport.Models; // Đảm bảo using namespace Models của bạn
using System.Collections.Generic;

namespace E_Sport.Models.ViewModels // Giữ nguyên namespace của bạn
{
    public class ProductFilterViewModel
    {
        // --- Các thuộc tính chung và giữ lại ---
        public IEnumerable<Product> Products { get; set; } // Danh sách sản phẩm sau khi lọc
        public List<Category> Categories { get; set; } // Danh sách tất cả danh mục để hiển thị bộ lọc
        public List<int> SelectedCategoryIds { get; set; } // Các ID danh mục người dùng đã chọn

        public decimal? MinPrice { get; set; } // Giá trị tối thiểu người dùng nhập/chọn
        public decimal? MaxPrice { get; set; } // Giá trị tối đa người dùng nhập/chọn

        // --- THÊM MỚI: Các thuộc tính cho bộ lọc Đặc Sản Địa Phương ---
        public List<string> AvailableOrigins { get; set; } // Danh sách các Xuất xứ có sẵn để hiển thị trong bộ lọc
        public List<string> SelectedOrigins { get; set; }  // Các Xuất xứ người dùng đã chọn để lọc

        public List<string> AvailableUnits { get; set; }   // Danh sách các Đơn vị tính có sẵn (tùy chọn, nếu bạn muốn lọc theo đơn vị)
        public List<string> SelectedUnits { get; set; }    // Các Đơn vị tính người dùng đã chọn (tùy chọn)

        // --- THÊM MỚI: Các thuộc tính để lưu và hiển thị lại trạng thái lọc hiện tại trên View ---
        public string? CurrentSearch { get; set; }      // Từ khóa tìm kiếm hiện tại
        public int? CurrentCategoryId { get; set; }      // ID danh mục đang được lọc (nếu chỉ cho chọn 1)
                                                         // Nếu cho chọn nhiều, SelectedCategoryIds đã đủ
        public string? CurrentOrigin { get; set; }      // Xuất xứ đang được lọc
        public string? CurrentUnit { get; set; }        // Đơn vị tính đang được lọc
        public string? CurrentPriceRange { get; set; }  // Chuỗi khoảng giá người dùng đã chọn (ví dụ "100000-500000")
        public string? CurrentSortBy { get; set; }      // Tiêu chí sắp xếp hiện tại

        // Constructor để khởi tạo các List, tránh lỗi NullReferenceException khi View cố gắng truy cập
        public ProductFilterViewModel()
        {
            Products = new List<Product>();
            Categories = new List<Category>();
            SelectedCategoryIds = new List<int>();

            AvailableOrigins = new List<string>();
            SelectedOrigins = new List<string>();

            AvailableUnits = new List<string>(); // Khởi tạo dù là tùy chọn
            SelectedUnits = new List<string>();  // Khởi tạo dù là tùy chọn
        }
    }
}
