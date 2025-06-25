using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq; // Thêm để sử dụng Sum trên OrderDetails

namespace E_Sport.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự.")]
        [Display(Name = "Tên Sản phẩm")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Giá bán không được để trống.")]
        [Range(1000, 100000000, ErrorMessage = "Giá phải từ 1.000đ đến 100.000.000đ.")]
        [DisplayFormat(DataFormatString = "{0:N0}đ", ApplyFormatInEditMode = false)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá bán")]
        public decimal Price { get; set; }

        [Display(Name = "Giá cũ (để trống nếu không giảm giá)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá cũ phải là số không âm.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldPrice { get; set; }

        [Display(Name = "Mô tả chi tiết")]
        public string Description { get; set; }

        [Display(Name = "Ảnh đại diện URL")]
        public string? ImageUrl { get; set; }

        public List<ProductImage>? Images { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục cho sản phẩm.")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Display(Name = "Vùng miền / Xuất xứ")]
        public int? RegionId { get; set; }
        [ForeignKey("RegionId")]
        public Region? Region { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Hạn sử dụng")]
        public DateTime? ExpiryDate { get; set; }

        [StringLength(50, ErrorMessage = "Thông tin trọng lượng không quá 50 ký tự.")]
        [Display(Name = "Trọng lượng / Quy cách")]
        public string? Weight { get; set; }

        [Display(Name = "Thành phần")]
        public string? Ingredients { get; set; }

        [Display(Name = "Hướng dẫn bảo quản")]
        public string? StorageInstructions { get; set; }

        [Display(Name = "Là thực phẩm tươi?")]
        public bool IsFreshFood { get; set; } = false;

        [StringLength(30, ErrorMessage = "Đơn vị tính không quá 30 ký tự.")]
        [Display(Name = "Đơn vị tính")]
        public string? Unit { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho không được để trống.")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được âm.")]
        [Display(Name = "Số lượng tồn")]
        public int StockQuantity { get; set; } = 0;

        [Display(Name = "Lượt xem")]
        public int ViewCount { get; set; } = 0;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "Hiển thị?")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Nổi bật?")]
        public bool IsFeatured { get; set; } = false;

        // NAVIGATION: các đơn hàng đã bán sản phẩm này
        public virtual ICollection<OrderDetail>? OrderDetails { get; set; } = new List<OrderDetail>();

        // Tính tổng số lượng đã bán (không lưu vào DB)
        [NotMapped]
        public int SoldCount
        {
            get
            {
                return OrderDetails?.Sum(od => od.Quantity) ?? 0;
            }
        }

        public Product()
        {
            Images = new List<ProductImage>();
            CreatedDate = DateTime.Now;
            IsActive = true;
        }

        // 2.1. Mã giảm giá
        public string? PromoCode { get; set; }

        // 2.2. Phí vận chuyển
        public decimal ShippingFee { get; set; }

        // 2.3. Danh sách reviews để EF.Include(p => p.Reviews) không lỗi
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>(); 

        // Tổng số lượng đã bán
        public int TotalSold { get; set; }

        // Các tuỳ chọn đóng gói
        public List<string> PackOptions { get; set; } = new List<string>();
    }
}
