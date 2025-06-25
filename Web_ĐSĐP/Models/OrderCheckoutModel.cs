using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Cần cho List

namespace E_Sport.Models // Giữ nguyên namespace của bạn
{
    public class OrderCheckoutModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên người nhận.")]
        [Display(Name = "Họ và tên người nhận")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại người nhận.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Display(Name = "Địa chỉ cụ thể (Số nhà, tên đường...)")]
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng cụ thể.")]
        public string ShippingAddress { get; set; }

        // Các thuộc tính này sẽ lưu mã của tỉnh/huyện/xã từ API
        [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố.")]
        [Display(Name = "Tỉnh/Thành phố (Mã)")]
        public string Province { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện.")]
        [Display(Name = "Quận/Huyện (Mã)")]
        public string District { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã.")]
        [Display(Name = "Phường/Xã (Mã)")]
        public string Ward { get; set; }

        // THÊM MỚI: Các thuộc tính để lưu tên Tỉnh/Huyện/Xã (để lưu vào Order hoặc hiển thị)
        [Display(Name = "Tên Tỉnh/Thành phố")]
        public string? ProvinceName { get; set; }

        [Display(Name = "Tên Quận/Huyện")]
        public string? DistrictName { get; set; }

        [Display(Name = "Tên Phường/Xã")]
        public string? WardName { get; set; }
        // Kết thúc thêm mới

        [Display(Name = "Ghi chú đơn hàng")]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; }


        // --- THÊM MỚI: Các thuộc tính để View Checkout.cshtml sử dụng ---
        public List<CartItem> CartItems { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }
        // --- Kết thúc thêm mới ---

        // Constructor để khởi tạo các List (quan trọng!)
        public OrderCheckoutModel()
        {
            CartItems = new List<CartItem>();
            // Khởi tạo giá trị mặc định nếu cần, ví dụ:
            FullName = string.Empty;
            Phone = string.Empty;
            ShippingAddress = string.Empty;
            Province = string.Empty;
            District = string.Empty;
            Ward = string.Empty;
            PaymentMethod = "COD"; // Mặc định là COD
        }
    }
}
