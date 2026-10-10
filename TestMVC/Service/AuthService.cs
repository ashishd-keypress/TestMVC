using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;
using Utility;

namespace TestMVC.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;

        public AuthService(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        public async Task<Tuple<RegisterRes, int, string>> RegisterAsync(RegisterReq model)
        {
            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password) || string.IsNullOrWhiteSpace(model.MobileNumber))
            {
                var errRes = new RegisterRes
                {
                    Success = false,
                    Message = "Username, password, and mobile number are all required."
                };
                return new Tuple<RegisterRes, int, string>(errRes, 0, string.Empty);
            }

            // Normalize and validate 10-digit Indian Mobile Number
            string cleanMobile = model.MobileNumber.Trim().Replace(" ", "").Replace("-", "");
            if (cleanMobile.StartsWith("+91")) cleanMobile = cleanMobile.Substring(3);
            if (cleanMobile.StartsWith("0") && cleanMobile.Length == 11) cleanMobile = cleanMobile.Substring(1);

            if (!System.Text.RegularExpressions.Regex.IsMatch(cleanMobile, @"^[6-9]\d{9}$"))
            {
                var errRes = new RegisterRes
                {
                    Success = false,
                    Message = "Please enter a valid 10-digit Indian mobile number (e.g. 9876543210)."
                };
                return new Tuple<RegisterRes, int, string>(errRes, 0, string.Empty);
            }
            model.MobileNumber = cleanMobile;

            // Password Complexity Validation (min 8 characters, capital, small, digit, special character)
            if (model.Password.Length < 8 ||
                !System.Text.RegularExpressions.Regex.IsMatch(model.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$"))
            {
                var errRes = new RegisterRes
                {
                    Success = false,
                    Message = "Password must be at least 8 characters long and contain at least one uppercase letter (A-Z), one lowercase letter (a-z), one digit (0-9), and one special character."
                };
                return new Tuple<RegisterRes, int, string>(errRes, 0, string.Empty);
            }

            // Securely hash password with unique salt
            PasswordSecurity.CreatePasswordHash(model.Password, out string hash, out string salt);

            return await _authRepository.RegisterAsync(model, hash, salt);
        }

        public async Task<Tuple<LoginRes, int, string>> LoginAsync(LoginReq model)
        {
            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                var errRes = new LoginRes
                {
                    Success = false,
                    Message = "Username and password are required."
                };
                return new Tuple<LoginRes, int, string>(errRes, 0, string.Empty);
            }

            return await _authRepository.ValidateLoginAsync(model);
        }

        public async Task<Tuple<List<UserItemRes>, int, string>> GetUsersAsync(GetUserReq model)
        {
            return await _authRepository.GetUsersAsync(model);
        }

        public async Task<Tuple<bool, int, string>> ToggleUserApprovalAsync(ToggleApprovalReq model)
        {
            return await _authRepository.ToggleUserApprovalAsync(model);
        }
    }
}
