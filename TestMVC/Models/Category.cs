using System.ComponentModel.DataAnnotations;

namespace TestMVC.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category Name is required.")]
        [Display(Name = "Category Name")]
        [StringLength(100, ErrorMessage = "Category Name cannot exceed 100 characters.")]
        public string CategoryName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsDelete { get; set; } = false;
        public int? UserId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
