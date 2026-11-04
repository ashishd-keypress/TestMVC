using System;
using System.Threading.Tasks;
using TestMVC.Models.ReqRes;

namespace TestMVC.Interface
{
    public interface IAuthService
    {
        Task<Tuple<LoginRes, int, string>> LoginAsync(LoginReq model);
    }
}
