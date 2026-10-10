using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models.ReqRes;

namespace TestMVC.Interface
{
    public interface IProductRepository
    {
        Task<Tuple<List<GetProductRes>, int, string>> GetProduct(GetProductReq model);
        Task<Tuple<GetProductRes?, int, string>> GetProductById(GetProductByIdReq model);
        Task<Tuple<bool, int, string>> SaveProduct(SaveProductReq model);
        Task<Tuple<ToggleStatusRes, int, string>> ToggleStatus(ToggleStatusReq model);
        Task<Tuple<DeleteRes, int, string>> DeleteProduct(DeleteReq model);
        Task<Tuple<int, int, string>> GetActiveProductCount(int? userId = null);
    }
}
