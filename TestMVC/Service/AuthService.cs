using System;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;

namespace TestMVC.Service
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;

        public AuthService(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
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
    }
}
