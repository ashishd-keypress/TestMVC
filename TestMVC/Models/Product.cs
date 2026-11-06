using Microsoft.AspNetCore.Http;
using System;
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

        [Display(Name = "Product Image")]
        public string? Image { get; set; }

        // Both Image and ImageUrl accessors supported
        [Display(Name = "Product Image")]
        public string? ImageUrl
        {
            get => Image;
            set => Image = value;
        }

        public string? ImagePath
        {
            get => Image;
            set => Image = value;
        }

        [Display(Name = "Upload Product Image")]
        public IFormFile? ImageFile { get; set; }

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

        public long? ProductModelId { get; set; }

        [Display(Name = "Model Name")]
        public string? ModelName { get; set; }

        [Display(Name = "SKU Code")]
        [StringLength(100)]
        public string? SKU { get; set; }

        [Display(Name = "Barcode")]
        [StringLength(100)]
        public string? Barcode { get; set; }

        [Display(Name = "RAM")]
        [StringLength(50)]
        public string? RAM { get; set; }

        [Display(Name = "Storage")]
        [StringLength(50)]
        public string? Storage { get; set; }

        [Display(Name = "Color")]
        [StringLength(50)]
        public string? Color { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, 100000000, ErrorMessage = "Price must be greater than or equal to 0.")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Display(Name = "MRP")]
        [Range(0, 100000000)]
        public decimal? MRP { get; set; }

        [Display(Name = "Selling Price")]
        [Range(0, 100000000)]
        public decimal? SellingPrice { get; set; }

        [Display(Name = "Warranty (Months)")]
        [Range(0, 120)]
        public int WarrantyMonths { get; set; } = 12;

        [Display(Name = "Track IMEI")]
        public bool TrackIMEI { get; set; } = false;

        [Display(Name = "Size / Variant")]
        [StringLength(100, ErrorMessage = "Size cannot exceed 100 characters.")]
        public string? Size { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;

        public bool IsDelete { get; set; } = false;

        public int? UserId { get; set; }

        public DateTime? CreatedDate { get; set; }

        [Display(Name = "In Stock")]
        public int StockQuantity { get; set; } = 0;
    }
}
