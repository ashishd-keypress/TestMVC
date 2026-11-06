using System;
using System.ComponentModel.DataAnnotations;

namespace TestMVC.Models
{
    public class DeviceUnit
    {
        public long DeviceUnitId { get; set; }

        [Required(ErrorMessage = "Please select a product.")]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        public string? ProductName { get; set; }
        public string? Brand { get; set; }
        public string? Category { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public string? Color { get; set; }

        public long? PurchaseItemId { get; set; }

        [Required(ErrorMessage = "IMEI 1 is required for mobile devices.")]
        [Display(Name = "IMEI 1")]
        [StringLength(50)]
        public string IMEI1 { get; set; } = string.Empty;

        [Display(Name = "IMEI 2")]
        [StringLength(50)]
        public string? IMEI2 { get; set; }

        [Display(Name = "Serial Number")]
        [StringLength(50)]
        public string? SerialNumber { get; set; }

        public long WarehouseId { get; set; } = 1;
        public string? WarehouseName { get; set; }

        /// <summary>
        /// 1 = In Stock, 2 = Reserved, 3 = Sold, 4 = Returned, 5 = Under Repair, 6 = Damaged
        /// </summary>
        [Display(Name = "Status")]
        public byte CurrentStatus { get; set; } = 1;

        public string StatusName => CurrentStatus switch
        {
            1 => "In Stock",
            2 => "Reserved",
            3 => "Sold",
            4 => "Returned",
            5 => "Under Repair",
            6 => "Damaged",
            _ => "Unknown"
        };

        [Display(Name = "Cost Price")]
        public decimal? CostPrice { get; set; }

        [Display(Name = "Selling Price")]
        public decimal? SellingPrice { get; set; }

        [Display(Name = "Warranty Start")]
        [DataType(DataType.Date)]
        public DateTime? WarrantyStartDate { get; set; }

        [Display(Name = "Warranty End")]
        [DataType(DataType.Date)]
        public DateTime? WarrantyEndDate { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
