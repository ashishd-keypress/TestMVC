using System;
using System.ComponentModel.DataAnnotations;

namespace TestMVC.Models
{
    public class Supplier
    {
        public long SupplierId { get; set; }

        [Required(ErrorMessage = "Supplier Name is required.")]
        [Display(Name = "Supplier / Company Name")]
        [StringLength(150)]
        public string SupplierName { get; set; } = string.Empty;

        [Display(Name = "Contact Person")]
        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "Phone number must contain only digits (10 to 15 digits).")]
        [Display(Name = "Phone")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 15 digits.")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [Display(Name = "GSTIN / Tax Number")]
        [StringLength(50)]
        public string? GSTNumber { get; set; }

        [Display(Name = "Address")]
        [StringLength(300)]
        public string? Address { get; set; }

        [Display(Name = "Opening Balance")]
        public decimal OpeningBalance { get; set; } = 0;

        [Display(Name = "Current Balance")]
        public decimal CurrentBalance { get; set; } = 0;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
