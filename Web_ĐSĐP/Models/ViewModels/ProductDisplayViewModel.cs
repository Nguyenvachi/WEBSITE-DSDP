using E_Sport.Models;
using System.Collections.Generic;

namespace E_Sport.Models.ViewModels
{
    public class ProductDisplayViewModel
    {
        public Product Product { get; set; }
        public List<Product> RelatedProducts { get; set; }

        // ↓↓↓ Các trường mới ↓↓↓
        public string PromoCode { get; set; }
        public decimal ShippingFee { get; set; }
        public string ShippingRegion { get; set; }

        public List<string> PackOptions { get; set; }
        public string SelectedPack { get; set; }

        // Review mới sẽ bind từ form
        public Review NewReview { get; set; } = new Review();

        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public int TotalSold { get; set; }

        public ProductDisplayViewModel()
        {
            Product = new Product();
            RelatedProducts = new List<Product>();

            PromoCode = string.Empty;
            ShippingFee = 0;
            ShippingRegion = string.Empty;

            PackOptions = new List<string>();
            SelectedPack = string.Empty;

            NewReview = new Review();

            TotalReviews = 0;
            AverageRating = 0;
            TotalSold = 0;
        }
    }
}
