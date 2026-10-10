using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;

namespace TestMVC.Service
{
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _brandRepository;

        public BrandService(IBrandRepository brandRepository)
        {
            _brandRepository = brandRepository;
        }

        public async Task<Tuple<List<GetBrandRes>, int, string>> GetBrand(GetBrandReq model)
        {
            return await _brandRepository.GetBrand(model);
        }

        public async Task<Tuple<GetBrandRes?, int, string>> GetBrandById(GetBrandByIdReq model)
        {
            return await _brandRepository.GetBrandById(model);
        }

        public async Task<Tuple<bool, int, string>> SaveBrand(SaveBrandReq model)
        {
            return await _brandRepository.SaveBrand(model);
        }

        public async Task<Tuple<ToggleStatusRes, int, string>> ToggleStatus(ToggleStatusReq model)
        {
            return await _brandRepository.ToggleStatus(model);
        }

        public async Task<Tuple<DeleteRes, int, string>> DeleteBrand(DeleteReq model)
        {
            return await _brandRepository.DeleteBrand(model);
        }
    }
}
