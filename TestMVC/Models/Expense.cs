using System;
using System.ComponentModel.DataAnnotations;

namespace TestMVC.Models
{
    public class Expense
    {
        public long ExpenseId { get; set; }

        [Required(ErrorMessage = "Please select an expense category.")]
        [Display(Name = "Category")]
        public int ExpenseCategoryId { get; set; }

        public string? CategoryName { get; set; }

        public long BranchId { get; set; } = 1;

        [Required(ErrorMessage = "Expense date is required.")]
        [Display(Name = "Expense Date")]
        [DataType(DataType.Date)]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 10000000, ErrorMessage = "Amount must be greater than 0.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Description / Title is required.")]
        [Display(Name = "Description")]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Payment Mode")]
        public int? PaymentMethodId { get; set; } = 1;

        public string? PaymentMethodName { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class ExpenseCategory
    {
        public int ExpenseCategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
