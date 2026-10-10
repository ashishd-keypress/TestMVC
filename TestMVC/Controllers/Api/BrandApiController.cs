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
    public class BrandController : ControllerBase
    {
        private readonly IBrandService _brandService;

        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        [HttpPost]
        [Route("get-brand")]
        public async Task<IActionResult> GetBrand([FromBody] GetBrandReq model)
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            List<GetBrandRes> response = new List<GetBrandRes>();
            int TotalRecords = 0;

            try
            {
                model ??= new GetBrandReq();
                model.UserId ??= Utility.Helper.GetUserId(User);
                (response, TotalRecords, StrDBError) = await _brandService.GetBrand(model);

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

                string path = string.Format("\\Brand\\GetBrand\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("get-brand-by-id")]
        public async Task<IActionResult> GetBrandById([FromBody] GetBrandByIdReq model)
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            GetBrandRes? response = null;
            int TotalRecords = 0;

            try
            {
                model.UserId ??= Utility.Helper.GetUserId(User);
                (response, TotalRecords, StrDBError) = await _brandService.GetBrandById(model);

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

                string path = string.Format("\\Brand\\GetBrandById\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("add-brand")]
        public async Task<IActionResult> AddBrand([FromBody] SaveBrandReq model)
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
                model.BrandId = 0;
                model.UserId ??= Utility.Helper.GetUserId(User);
                (success, TotalRecords, StrDBError) = await _brandService.SaveBrand(model);

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

                string path = string.Format("\\Brand\\AddBrand\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("update-brand")]
        public async Task<IActionResult> UpdateBrand([FromBody] SaveBrandReq model)
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
                (success, TotalRecords, StrDBError) = await _brandService.SaveBrand(model);

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

                string path = string.Format("\\Brand\\UpdateBrand\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
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
                (response, TotalRecords, StrDBError) = await _brandService.ToggleStatus(model);

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

                string path = string.Format("\\Brand\\ToggleStatus\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("delete-brand")]
        [Route("delete-brand/{id}")]
        [Route("Delete/{id}")]
        public async Task<IActionResult> DeleteBrand([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] DeleteReq? model, int? id)
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
                (response, TotalRecords, StrDBError) = await _brandService.DeleteBrand(model);

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

                string path = string.Format("\\Brand\\DeleteBrand\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }
    }
}
