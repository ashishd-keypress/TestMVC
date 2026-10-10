using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models.ReqRes;

namespace TestMVC.Interface
{
    public interface IAuthService
    {
        Task<Tuple<RegisterRes, int, string>> RegisterAsync(RegisterReq model);
        Task<Tuple<LoginRes, int, string>> LoginAsync(LoginReq model);
        Task<Tuple<List<UserItemRes>, int, string>> GetUsersAsync(GetUserReq model);
        Task<Tuple<bool, int, string>> ToggleUserApprovalAsync(ToggleApprovalReq model);
    }
}
