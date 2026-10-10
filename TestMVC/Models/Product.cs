using System.ComponentModel.DataAnnotations;

namespace TestMVC.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product Name is required.")]
        [Display(Name = "Product Name")]
        [StringLength(500, ErrorMessage = "Product Name cannot exceed 500 characters.")]
        public string Name { get; set; } = string.Empty;

        // Alias for backwards compatibility
        public string ProductName
        {
            get => Name;
            set => Name = value;
        }

        [Required(ErrorMessage = "Please select a Brand.")]
        [Display(Name = "Brand")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Brand.")]
        public int BrandId { get; set; }

        [Display(Name = "Brand")]
        public string? Brand { get; set; }

        [Required(ErrorMessage = "Please select a Category.")]
        [Display(Name = "Category")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Category.")]
        public int CategoryId { get; set; }

        [Display(Name = "Category")]
        public string? Category { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, 100000000, ErrorMessage = "Price must be greater than or equal to 0.")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Display(Name = "Size")]
        [StringLength(100, ErrorMessage = "Size cannot exceed 100 characters.")]
        public string? Size { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;

        public bool IsDelete { get; set; } = false;

        public int? UserId { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
}
