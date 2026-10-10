using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;

namespace TestMVC.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Tuple<List<GetProductRes>, int, string>> GetProduct(GetProductReq model)
        {
            return await _productRepository.GetProduct(model);
        }

        public async Task<Tuple<GetProductRes?, int, string>> GetProductById(GetProductByIdReq model)
        {
            return await _productRepository.GetProductById(model);
        }

        public async Task<Tuple<bool, int, string>> SaveProduct(SaveProductReq model)
        {
            return await _productRepository.SaveProduct(model);
        }

        public async Task<Tuple<ToggleStatusRes, int, string>> ToggleStatus(ToggleStatusReq model)
        {
            return await _productRepository.ToggleStatus(model);
        }

        public async Task<Tuple<DeleteRes, int, string>> DeleteProduct(DeleteReq model)
        {
            return await _productRepository.DeleteProduct(model);
        }
    }
}
