using System;
using System.ComponentModel.DataAnnotations;

namespace TestMVC.Models.ReqRes
{
    public class RegisterReq
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter (A-Z), one lowercase letter (a-z), one digit (0-9), and one special character.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required.")]
        [RegularExpression(@"^(?:\+91)?[6-9]\d{9}$", ErrorMessage = "Please enter a valid 10-digit Indian mobile number (e.g. 9876543210).")]
        public string MobileNumber { get; set; } = string.Empty;
    }

    public class RegisterRes
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = false;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class LoginReq
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRes
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
    }

    public class UserItemRes
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class GetUserReq
    {
        public string? SearchText { get; set; }
        public bool? IsApproved { get; set; }
        public int PageNo { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class ToggleApprovalReq
    {
        public int UserId { get; set; }
        public bool? IsApproved { get; set; }
    }
}
