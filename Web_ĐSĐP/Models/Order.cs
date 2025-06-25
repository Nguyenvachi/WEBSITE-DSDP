using System.ComponentModel.DataAnnotations;

namespace E_Sport.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserEmail { get; set; } = "";
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";
        public List<OrderDetail>? OrderDetails { get; set; }
        public string FullName { get; internal set; }
        //public string Address { get; internal set; }
        public string? Note { get; internal set; }
        public string Phone { get; internal set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; } = "COD";

    }
}
