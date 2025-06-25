// File: Models/Region.cs
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace E_Sport.Models // Giữ nguyên namespace của bạn
{
    public class Region
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên vùng miền không được để trống.")]
        [StringLength(150, ErrorMessage = "Tên vùng miền không được vượt quá 150 ký tự.")]
        [Display(Name = "Tên Vùng miền / Xuất xứ")]
        public string Name { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; } // Mô tả thêm về vùng miền (ví dụ: đặc điểm địa lý, văn hóa ảnh hưởng đến đặc sản)

        [StringLength(255)]
        [Display(Name = "URL Hình ảnh đại diện Vùng miền")]
        public string? ImageUrl { get; set; } // Hình ảnh đặc trưng cho vùng miền

        [Display(Name = "Hiển thị?")]
        public bool IsActive { get; set; } = true; // Cho phép ẩn/hiện vùng miền trong các bộ lọc hoặc danh sách

        // Navigation property: Danh sách các sản phẩm thuộc vùng miền này
        // Điều này thiết lập mối quan hệ một (Region) - nhiều (Products)
        public List<Product>? Products { get; set; }

        public Region()
        {
            Products = new List<Product>();
            IsActive = true;
        }
    }
}
