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
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginReq model)
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            LoginRes response = new LoginRes();
            int TotalRecords = 0;

            try
            {
                model ??= new LoginReq();

                if (!ModelState.IsValid)
                {
                    tranStatus.Code = 2;
                    tranStatus.ReturnMessage = "Username and password are required.";
                    statusCode = HttpStatusCode.BadRequest;
                }
                else
                {
                    (response, TotalRecords, StrDBError) = await _authService.LoginAsync(model);

                    if (!string.IsNullOrEmpty(StrDBError))
                    {
                        tranStatus.ReturnMessage = "Something went wrong(DB).";
                        tranStatus.Code = 2;
                        statusCode = HttpStatusCode.InternalServerError;
                    }
                    else if (!response.Success)
                    {
                        tranStatus.Code = 2;
                        tranStatus.ReturnMessage = response.Message;
                        statusCode = HttpStatusCode.Unauthorized;
                    }
                    else
                    {
                        tranStatus.ReturnMessage = response.Message;
                    }
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
                dctData.Add("success", response.Success);
                dctData.Add("message", response.Message);
                dctData.Add("token", response.Token);

                // Sanitize password before logging for security
                var logModel = new { Username = model?.Username, Password = "***" };

                string message = "*******************************************************************" + Environment.NewLine
                                 + "Request UTC: " + reqDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Response UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Request: " + JsonConvert.SerializeObject(logModel) + Environment.NewLine
                                 + "Response: " + JsonConvert.SerializeObject(dctData) + Environment.NewLine
                                 + "Message: " + StrSystemError + Environment.NewLine
                                 + "DataBase Error: " + StrDBError + Environment.NewLine;

                string path = string.Format("\\Auth\\Login\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }
    }
}
