using System.ComponentModel.DataAnnotations; // Có thể cần nếu bạn muốn thêm validation sau này

namespace E_Sport.Models // Giữ nguyên namespace của bạn
{
    public class CartItem
    {
        public int ProductId { get; set; }

        public string? ProductName { get; set; } // Tên sản phẩm tại thời điểm thêm vào giỏ

        public string? ImageUrl { get; set; } // URL ảnh sản phẩm

        [DataType(DataType.Currency)] // Giúp định dạng tiền tệ
        public decimal Price { get; set; } // Giá của một đơn vị sản phẩm tại thời điểm thêm vào giỏ

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải ít nhất là 1")]
        public int Quantity { get; set; }

        public decimal Total => Price * Quantity; // Tính toán tổng tiền cho item này

        // public string? Size { get; set; } // ĐÃ XÓA HOẶC COMMENT LẠI

        // THÊM MỚI (Tùy chọn nhưng hữu ích): Đơn vị tính của sản phẩm
        public string? Unit { get; set; } // Ví dụ: "kg", "gói", "hộp", "chai"
                                          // Sẽ được lấy từ Product.Unit khi thêm vào giỏ
    }
}
