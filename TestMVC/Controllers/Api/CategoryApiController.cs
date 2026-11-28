using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;
using TestMVC.Models.ReqRes;
using Utility;

namespace TestMVC.Controllers.Api
{
    [ApiController]
    [Route("api/Category")]
    public class CategoryApiController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _env;

        public CategoryApiController(ICategoryService categoryService, IWebHostEnvironment env)
        {
            _categoryService = categoryService;
            _env = env;
        }

        [HttpGet]
        [Route("category_list_get")]
        public async Task<IActionResult> Search([FromQuery] string? search)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var (categoriesRes, total, err) = await _categoryService.GetCategory(new GetCategoryReq { SearchText = search, PageSize = 100, UserId = currentUserId });
            return Ok(new
            {
                success = string.IsNullOrEmpty(err),
                data = categoriesRes.Select(c => new
                {
                    categoryId = c.CategoryId,
                    categoryName = c.CategoryName,
                    imageUrl = c.ImageUrl,
                    isActive = c.IsActive,
                    userId = c.UserId,
                    createdDate = c.CreatedDate.HasValue ? c.CreatedDate.Value.ToString("yyyy-MM-dd hh:mm tt") : "-"
                }),
                totalRecords = total
            });
        }

        [HttpPost]
        [Route("category_status_update")]
        public async Task<IActionResult> ToggleStatus([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ToggleStatusReq? model, [FromQuery(Name = "id")] int? queryId = null, int? id = null)
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            ToggleStatusRes response = new ToggleStatusRes();
            int TotalRecords = 0;

            model ??= new ToggleStatusReq();
            if ((queryId ?? 0) > 0)
            {
                model.Id = queryId!.Value;
            }
            else if (id.HasValue && id.Value > 0)
            {
                model.Id = id.Value;
            }
            model.UserId ??= Utility.Helper.GetUserId(User);

            try
            {
                (response, TotalRecords, StrDBError) = await _categoryService.ToggleStatus(model);

                if (!string.IsNullOrEmpty(StrDBError))
                {
                    tranStatus.ReturnMessage = "Something went wrong(DB).";
                    tranStatus.Code = 2;
                }
            }
            catch (Exception ex)
            {
                tranStatus.Code = 2;
                tranStatus.ReturnMessage = "Something went wrong.";
                statusCode = HttpStatusCode.BadRequest;
                StrSystemError = JsonConvert.SerializeObject(ex);
            }
            finally
            {
                dctData.Add("Data", response);
                dctData.Add("TotalRecords", TotalRecords);
                dctData.Add("Status", tranStatus);
                // Also provide top-level JSON fields for client convenience
                dctData.Add("success", response.Success);
                dctData.Add("isActive", response.IsActive);
                dctData.Add("message", response.Message);

                string message = "*******************************************************************" + Environment.NewLine
                                 + "Request UTC: " + reqDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Response UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Request: " + JsonConvert.SerializeObject(model) + Environment.NewLine
                                 + "Response: " + JsonConvert.SerializeObject(dctData) + Environment.NewLine
                                 + "Message: " + StrSystemError + Environment.NewLine
                                 + "DataBase Error: " + StrDBError + Environment.NewLine;

                string path = string.Format("\\Category\\ToggleStatus\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("category_detail_create")]
        public async Task<IActionResult> SaveForm([FromForm] Category category)
        {
            var (imgValid, imgErr) = FileHelper.ValidateImage(category.ImageFile, isRequired: true);
            if (!imgValid)
            {
                ModelState.AddModelError("ImageFile", imgErr!);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return BadRequest(new { success = false, message = imgErr ?? "Validation failed.", errors });
            }

            string? savedImageUrl = null;
            if (category.ImageFile != null && category.ImageFile.Length > 0)
            {
                savedImageUrl = await FileHelper.SaveImageAsync(category.ImageFile, _env, "categories");
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (success, _, err) = await _categoryService.SaveCategory(new SaveCategoryReq
            {
                CategoryId = 0,
                CategoryName = category.CategoryName,
                ImageUrl = savedImageUrl,
                IsActive = category.IsActive,
                UserId = currentUserId
            });

            if (success)
            {
                return StatusCode(201, new { success = true, message = "Category added successfully.", redirectUrl = "/Category" });
            }

            if (!string.IsNullOrEmpty(savedImageUrl))
            {
                FileHelper.DeleteImage(savedImageUrl, _env);
            }

            string errorMsg = !string.IsNullOrEmpty(err) && !err.StartsWith("{")
                ? err
                : "Unable to add category. Please check the entered details and try again.";
            return BadRequest(new { success = false, message = errorMsg });
        }

        [HttpPut]
        [Route("category_detail_update")]
        public async Task<IActionResult> UpdateForm([FromForm] Category category, [FromQuery(Name = "id")] int? queryId = null, int id = 0)
        {
            int explicitId = (queryId ?? 0) > 0 ? queryId!.Value : id;
            int targetId = explicitId > 0 ? explicitId : category.CategoryId;
            if (targetId <= 0 || (explicitId > 0 && category.CategoryId > 0 && explicitId != category.CategoryId))
            {
                return BadRequest(new { success = false, message = "Invalid category ID." });
            }

            category.CategoryId = targetId;
            int? currentUserId = Utility.Helper.GetUserId(User);
            var (existing, _, _) = await _categoryService.GetCategoryById(new GetCategoryByIdReq { CategoryId = targetId, UserId = currentUserId });
            if (existing == null)
            {
                return NotFound(new { success = false, message = "Category not found." });
            }

            bool hasExisting = !string.IsNullOrEmpty(existing.ImageUrl);
            var (imgValid, imgErr) = FileHelper.ValidateImage(category.ImageFile, isRequired: !hasExisting);
            if (!imgValid)
            {
                ModelState.AddModelError("ImageFile", imgErr!);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return BadRequest(new { success = false, message = imgErr ?? "Validation failed.", errors });
            }

            string finalImageUrl = existing.ImageUrl ?? string.Empty;
            if (category.ImageFile != null && category.ImageFile.Length > 0)
            {
                finalImageUrl = await FileHelper.SaveImageAsync(category.ImageFile, _env, "categories");
                FileHelper.DeleteImage(existing.ImageUrl, _env);
            }

            var (success, _, err) = await _categoryService.SaveCategory(new SaveCategoryReq
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                ImageUrl = finalImageUrl,
                IsActive = category.IsActive,
                UserId = currentUserId
            });

            if (success)
            {
                return Ok(new { success = true, message = "Category updated successfully.", redirectUrl = "/Category" });
            }

            string errorMsg = !string.IsNullOrEmpty(err) && !err.StartsWith("{")
                ? err
                : "Unable to update category. Please check the entered details and try again.";
            return BadRequest(new { success = false, message = errorMsg });
        }

        [HttpDelete]
        [Route("category_delete")]
        public async Task<IActionResult> DeleteCategory([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] DeleteReq? model, [FromQuery(Name = "id")] int? queryId = null, int? id = null)
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            DeleteRes response = new DeleteRes();
            int TotalRecords = 0;

            model ??= new DeleteReq();
            if ((queryId ?? 0) > 0)
            {
                model.Id = queryId!.Value;
            }
            else if ((id ?? 0) > 0)
            {
                model.Id = id!.Value;
            }
            model.UserId ??= Utility.Helper.GetUserId(User);

            try
            {
                (response, TotalRecords, StrDBError) = await _categoryService.DeleteCategory(model);

                if (!string.IsNullOrEmpty(StrDBError))
                {
                    tranStatus.ReturnMessage = "Something went wrong(DB).";
                    tranStatus.Code = 2;
                }
            }
            catch (Exception ex)
            {
                tranStatus.Code = 2;
                tranStatus.ReturnMessage = "Something went wrong.";
                statusCode = HttpStatusCode.BadRequest;
                StrSystemError = JsonConvert.SerializeObject(ex);
            }
            finally
            {
                dctData.Add("Data", response);
                dctData.Add("TotalRecords", TotalRecords);
                dctData.Add("Status", tranStatus);
                // Also provide top-level JSON fields for client convenience
                dctData.Add("success", response.Success);
                dctData.Add("message", response.Message);

                string message = "*******************************************************************" + Environment.NewLine
                                 + "Request UTC: " + reqDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Response UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Request: " + JsonConvert.SerializeObject(model) + Environment.NewLine
                                 + "Response: " + JsonConvert.SerializeObject(dctData) + Environment.NewLine
                                 + "Message: " + StrSystemError + Environment.NewLine
                                 + "DataBase Error: " + StrDBError + Environment.NewLine;

                string path = string.Format("\\Category\\DeleteCategory\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }
    }
}
