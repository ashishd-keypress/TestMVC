using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models.ReqRes;

namespace TestMVC.Interface
{
    public interface IAuthRepository
    {
        Task EnsureUserTableCreatedAsync();
        Task<Tuple<RegisterRes, int, string>> RegisterAsync(RegisterReq model, string passwordHash, string passwordSalt);
        Task<Tuple<LoginRes, int, string>> ValidateLoginAsync(LoginReq model);
        Task<Tuple<List<UserItemRes>, int, string>> GetUsersAsync(GetUserReq model);
        Task<Tuple<bool, int, string>> ToggleUserApprovalAsync(ToggleApprovalReq model);
    }
}
