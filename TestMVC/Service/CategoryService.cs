using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;

namespace TestMVC.Service
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Tuple<List<GetCategoryRes>, int, string>> GetCategory(GetCategoryReq model)
        {
            return await _categoryRepository.GetCategory(model);
        }

        public async Task<Tuple<GetCategoryRes?, int, string>> GetCategoryById(GetCategoryByIdReq model)
        {
            return await _categoryRepository.GetCategoryById(model);
        }

        public async Task<Tuple<bool, int, string>> SaveCategory(SaveCategoryReq model)
        {
            return await _categoryRepository.SaveCategory(model);
        }

        public async Task<Tuple<ToggleStatusRes, int, string>> ToggleStatus(ToggleStatusReq model)
        {
            return await _categoryRepository.ToggleStatus(model);
        }

        public async Task<Tuple<DeleteRes, int, string>> DeleteCategory(DeleteReq model)
        {
            return await _categoryRepository.DeleteCategory(model);
        }
    }
}
