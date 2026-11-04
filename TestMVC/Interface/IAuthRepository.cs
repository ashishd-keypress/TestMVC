using System;
using System.Threading.Tasks;
using TestMVC.Models.ReqRes;

namespace TestMVC.Interface
{
    public interface IAuthRepository
    {
        Task EnsureUserTableCreatedAsync();
        Task<Tuple<LoginRes, int, string>> ValidateLoginAsync(LoginReq model);
    }
}
