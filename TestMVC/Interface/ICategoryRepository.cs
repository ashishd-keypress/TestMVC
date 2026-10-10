using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models.ReqRes;

namespace TestMVC.Interface
{
    public interface ICategoryRepository
    {
        Task<Tuple<List<GetCategoryRes>, int, string>> GetCategory(GetCategoryReq model);
        Task<Tuple<GetCategoryRes?, int, string>> GetCategoryById(GetCategoryByIdReq model);
        Task<Tuple<bool, int, string>> SaveCategory(SaveCategoryReq model);
        Task<Tuple<ToggleStatusRes, int, string>> ToggleStatus(ToggleStatusReq model);
        Task<Tuple<DeleteRes, int, string>> DeleteCategory(DeleteReq model);
        Task<Tuple<int, int, string>> GetActiveCategoryCount(int? userId = null);
    }
}
