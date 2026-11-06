using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TestMVC.Models
{
    public class Purchase
    {
        public long PurchaseId { get; set; }

        [Required(ErrorMessage = "Please select a supplier.")]
        [Display(Name = "Supplier")]
        public long SupplierId { get; set; }

        public string? SupplierName { get; set; }
        public string? SupplierPhone { get; set; }

        public long BranchId { get; set; } = 1;

        [Required(ErrorMessage = "Purchase Invoice / Bill Number is required.")]
        [Display(Name = "Bill / Invoice No.")]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Display(Name = "Purchase Date")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        [Display(Name = "Sub Total")]
        public decimal SubTotal { get; set; } = 0;

        [Display(Name = "Discount")]
        public decimal Discount { get; set; } = 0;

        [Display(Name = "Tax / GST")]
        public decimal TaxAmount { get; set; } = 0;

        [Display(Name = "Grand Total")]
        public decimal GrandTotal { get; set; } = 0;

        [Display(Name = "Paid Amount")]
        public decimal PaidAmount { get; set; } = 0;

        [Display(Name = "Due Amount")]
        public decimal DueAmount { get; set; } = 0;

        [Display(Name = "Status")]
        public string Status { get; set; } = "Received";

        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public List<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
    }

    public class PurchaseItem
    {
        public long PurchaseItemId { get; set; }
        public long PurchaseId { get; set; }

        [Required]
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public bool TrackIMEI { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal UnitCost { get; set; } = 0;
        public decimal Discount { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; } = 0;

        /// <summary>
        /// Comma or newline separated list of IMEIs entered during stock inward
        /// </summary>
        public string? IMEINumbers { get; set; }
    }
}
