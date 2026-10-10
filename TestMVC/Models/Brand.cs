using System.ComponentModel.DataAnnotations;

namespace TestMVC.Models
{
    public class Brand
    {
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Brand Name is required.")]
        [Display(Name = "Brand Name")]
        [StringLength(100, ErrorMessage = "Brand Name cannot exceed 100 characters.")]
        public string BrandName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsDelete { get; set; } = false;
        public int? UserId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
