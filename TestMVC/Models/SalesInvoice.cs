using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TestMVC.Models
{
    public class SalesInvoice
    {
        public long SalesInvoiceId { get; set; }

        public long BranchId { get; set; } = 1;

        [Display(Name = "Customer")]
        public long? CustomerId { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerGST { get; set; }

        [Display(Name = "Invoice Number")]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Display(Name = "Invoice Date")]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

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

        [Display(Name = "Payment Mode")]
        public int PaymentMethodId { get; set; } = 1;

        public string? PaymentMethodName { get; set; }

        [Display(Name = "Transaction Ref / UPI Ref")]
        public string? TransactionReference { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Completed";

        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public List<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
    }

    public class SalesInvoiceItem
    {
        public long SalesInvoiceItemId { get; set; }
        public long SalesInvoiceId { get; set; }

        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public string? Color { get; set; }
        public bool TrackIMEI { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; } = 0;
        public decimal Discount { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; } = 0;

        // Selected Device / IMEI for this sale item
        public long? DeviceUnitId { get; set; }
        public string? IMEI1 { get; set; }
        public string? IMEI2 { get; set; }
    }
}
