using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;

namespace TestMVC.Controllers.Api
{
    [ApiController]
    [Route("api/PaymentMethod")]
    public class PaymentMethodApiController : ControllerBase
    {
        private readonly ISalesService _salesService;

        public PaymentMethodApiController(ISalesService salesService)
        {
            _salesService = salesService;
        }

        [HttpGet]
        [Route("payment_method_list_get")]
        public async Task<IActionResult> GetAll()
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var methods = await _salesService.GetPaymentMethods(currentUserId);
            return Ok(new
            {
                success = true,
                data = methods.Select(m => new
                {
                    paymentMethodId = m.PaymentMethodId,
                    methodName = m.MethodName,
                    isActive = m.IsActive,
                    userId = m.UserId
                }),
                totalRecords = methods.Count,
                message = "Payment modes retrieved successfully."
            });
        }

        [HttpGet]
        [Route("payment_method_detail_get")]
        public async Task<IActionResult> GetById([FromQuery(Name = "id")] int? queryId = null, int id = 0)
        {
            int targetId = (queryId ?? 0) > 0 ? queryId!.Value : id;
            if (targetId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid payment mode identifier." });
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var method = await _salesService.GetPaymentMethodById(targetId, currentUserId);
            if (method == null)
            {
                return NotFound(new { success = false, message = "Payment mode not found." });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    paymentMethodId = method.PaymentMethodId,
                    methodName = method.MethodName,
                    isActive = method.IsActive,
                    userId = method.UserId
                }
            });
        }

        [HttpPost]
        [Route("payment_method_create")]
        public async Task<IActionResult> Create([FromBody] PaymentMethod? paymentMethod)
        {
            if (paymentMethod == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return BadRequest(new { success = false, message = "Validation failed.", errors });
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            paymentMethod.PaymentMethodId = 0;
            paymentMethod.UserId = currentUserId;

            var (success, message, saved) = await _salesService.SavePaymentMethod(paymentMethod);
            if (!success || saved == null)
            {
                return BadRequest(new { success = false, message });
            }

            return StatusCode(201, new
            {
                success = true,
                message,
                data = new
                {
                    paymentMethodId = saved.PaymentMethodId,
                    methodName = saved.MethodName,
                    isActive = saved.IsActive,
                    userId = saved.UserId
                }
            });
        }

        [HttpPut]
        [Route("payment_method_update")]
        public async Task<IActionResult> Update([FromBody] PaymentMethod? paymentMethod, [FromQuery(Name = "id")] int? queryId = null, int id = 0)
        {
            if (paymentMethod == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if ((queryId ?? 0) > 0)
            {
                paymentMethod.PaymentMethodId = queryId!.Value;
            }
            else if (id > 0)
            {
                paymentMethod.PaymentMethodId = id;
            }

            if (paymentMethod.PaymentMethodId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid PaymentMethodId is required for update." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return BadRequest(new { success = false, message = "Validation failed.", errors });
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            paymentMethod.UserId = currentUserId;

            var (success, message, saved) = await _salesService.SavePaymentMethod(paymentMethod);
            if (!success || saved == null)
            {
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, message });
                }
                return BadRequest(new { success = false, message });
            }

            return Ok(new
            {
                success = true,
                message,
                data = new
                {
                    paymentMethodId = saved.PaymentMethodId,
                    methodName = saved.MethodName,
                    isActive = saved.IsActive,
                    userId = saved.UserId
                }
            });
        }

        [HttpDelete]
        [Route("payment_method_delete")]
        public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] DeleteReq? model = null, [FromQuery(Name = "id")] int? queryId = null, int id = 0)
        {
            int targetId = (queryId ?? 0) > 0 ? queryId!.Value : (id > 0 ? id : (model != null && model.Id > 0 ? model.Id : 0));
            if (targetId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid payment mode identifier." });
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (success, message) = await _salesService.DeletePaymentMethod(targetId, currentUserId);
            if (!success)
            {
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, message });
                }
                return BadRequest(new { success = false, message });
            }

            return Ok(new
            {
                success = true,
                message
            });
        }
    }
}
