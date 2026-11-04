using System;
using System.ComponentModel.DataAnnotations;

namespace TestMVC.Models.ReqRes
{
    public class LoginReq
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        public string? ValidationCode { get; set; }

        public string? ExpectedValidationCode { get; set; }
    }

    public class LoginRes
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
    }
}
