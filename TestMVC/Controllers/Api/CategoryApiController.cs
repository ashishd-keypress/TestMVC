using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;
using Utility;

namespace TestMVC.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpPost]
        [Route("get-category")]
        public async Task<IActionResult> GetCategory([FromBody] GetCategoryReq model)
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            List<GetCategoryRes> response = new List<GetCategoryRes>();
            int TotalRecords = 0;

            try
            {
                model ??= new GetCategoryReq();
                model.UserId ??= Utility.Helper.GetUserId(User);
                (response, TotalRecords, StrDBError) = await _categoryService.GetCategory(model);

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

                string message = "*******************************************************************" + Environment.NewLine
                                 + "Request UTC: " + reqDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Response UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Request: " + JsonConvert.SerializeObject(model) + Environment.NewLine
                                 + "Response: " + JsonConvert.SerializeObject(dctData) + Environment.NewLine
                                 + "Message: " + StrSystemError + Environment.NewLine
                                 + "DataBase Error: " + StrDBError + Environment.NewLine;

                string path = string.Format("\\Category\\GetCategory\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("get-category-by-id")]
        public async Task<IActionResult> GetCategoryById([FromBody] GetCategoryByIdReq model)
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            GetCategoryRes? response = null;
            int TotalRecords = 0;

            try
            {
                model.UserId ??= Utility.Helper.GetUserId(User);
                (response, TotalRecords, StrDBError) = await _categoryService.GetCategoryById(model);

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

                string message = "*******************************************************************" + Environment.NewLine
                                 + "Request UTC: " + reqDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Response UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Request: " + JsonConvert.SerializeObject(model) + Environment.NewLine
                                 + "Response: " + JsonConvert.SerializeObject(dctData) + Environment.NewLine
                                 + "Message: " + StrSystemError + Environment.NewLine
                                 + "DataBase Error: " + StrDBError + Environment.NewLine;

                string path = string.Format("\\Category\\GetCategoryById\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("add-category")]
        public async Task<IActionResult> AddCategory([FromBody] SaveCategoryReq model)
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            bool success = false;
            int TotalRecords = 0;

            try
            {
                model.CategoryId = 0;
                model.UserId ??= Utility.Helper.GetUserId(User);
                (success, TotalRecords, StrDBError) = await _categoryService.SaveCategory(model);

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
                dctData.Add("Data", success);
                dctData.Add("TotalRecords", TotalRecords);
                dctData.Add("Status", tranStatus);

                string message = "*******************************************************************" + Environment.NewLine
                                 + "Request UTC: " + reqDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Response UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Request: " + JsonConvert.SerializeObject(model) + Environment.NewLine
                                 + "Response: " + JsonConvert.SerializeObject(dctData) + Environment.NewLine
                                 + "Message: " + StrSystemError + Environment.NewLine
                                 + "DataBase Error: " + StrDBError + Environment.NewLine;

                string path = string.Format("\\Category\\AddCategory\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("update-category")]
        public async Task<IActionResult> UpdateCategory([FromBody] SaveCategoryReq model)
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            bool success = false;
            int TotalRecords = 0;

            try
            {
                model.UserId ??= Utility.Helper.GetUserId(User);
                (success, TotalRecords, StrDBError) = await _categoryService.SaveCategory(model);

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
                dctData.Add("Data", success);
                dctData.Add("TotalRecords", TotalRecords);
                dctData.Add("Status", tranStatus);

                string message = "*******************************************************************" + Environment.NewLine
                                 + "Request UTC: " + reqDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Response UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Request: " + JsonConvert.SerializeObject(model) + Environment.NewLine
                                 + "Response: " + JsonConvert.SerializeObject(dctData) + Environment.NewLine
                                 + "Message: " + StrSystemError + Environment.NewLine
                                 + "DataBase Error: " + StrDBError + Environment.NewLine;

                string path = string.Format("\\Category\\UpdateCategory\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("toggle-status")]
        [Route("toggle-status/{id}")]
        [Route("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ToggleStatusReq? model, int? id)
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
            if (id.HasValue && id.Value > 0)
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
        [Route("delete-category")]
        [Route("delete-category/{id}")]
        [Route("Delete/{id}")]
        public async Task<IActionResult> DeleteCategory([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] DeleteReq? model, int? id)
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
            if (id.HasValue && id.Value > 0)
            {
                model.Id = id.Value;
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
