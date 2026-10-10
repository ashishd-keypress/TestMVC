using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models.ReqRes;

namespace TestMVC.Interface
{
    public interface IBrandRepository
    {
        Task<Tuple<List<GetBrandRes>, int, string>> GetBrand(GetBrandReq model);
        Task<Tuple<GetBrandRes?, int, string>> GetBrandById(GetBrandByIdReq model);
        Task<Tuple<bool, int, string>> SaveBrand(SaveBrandReq model);
        Task<Tuple<ToggleStatusRes, int, string>> ToggleStatus(ToggleStatusReq model);
        Task<Tuple<DeleteRes, int, string>> DeleteBrand(DeleteReq model);
        Task<Tuple<int, int, string>> GetActiveBrandCount(int? userId = null);
    }
}
