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
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _env;

        public ProductController(IProductService productService, IWebHostEnvironment env)
        {
            _productService = productService;
            _env = env;
        }

        [HttpPost]
        [Route("get-product")]
        public async Task<IActionResult> GetProduct([FromBody] GetProductReq model)
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            List<GetProductRes> response = new List<GetProductRes>();
            int TotalRecords = 0;

            try
            {
                model ??= new GetProductReq();
                model.UserId ??= Utility.Helper.GetUserId(User);
                (response, TotalRecords, StrDBError) = await _productService.GetProduct(model);

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

                string path = string.Format("\\Product\\GetProduct\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpGet]
        [Route("")]
        [Route("product_list_get")]
        [Route("search")]
        [Route("SearchApi")]
        public async Task<IActionResult> Search([FromQuery] string? search, [FromQuery] int pageNo = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNo < 1) pageNo = 1;
            var req = new GetProductReq { SearchText = search, PageNo = pageNo, PageSize = pageSize, UserId = Utility.Helper.GetUserId(User) };
            var (productsRes, total, err) = await _productService.GetProduct(req);
            bool hasNext = (pageNo * pageSize) < total;

            return Ok(new
            {
                success = string.IsNullOrEmpty(err),
                data = productsRes.Select(p => new
                {
                    productId = p.ProductId,
                    name = p.Name,
                    image = p.Image ?? p.ImageUrl,
                    imageUrl = p.Image ?? p.ImageUrl,
                    brandId = p.BrandId,
                    brand = p.Brand,
                    categoryId = p.CategoryId,
                    category = p.Category,
                    price = p.Price,
                    mrp = p.MRP,
                    sellingPrice = p.SellingPrice,
                    size = p.Size,
                    ram = p.RAM,
                    storage = p.Storage,
                    color = p.Color,
                    sku = p.SKU,
                    barcode = p.Barcode,
                    warrantyMonths = p.WarrantyMonths,
                    trackImei = p.TrackIMEI,
                    stockQuantity = p.StockQuantity,
                    isActive = p.IsActive,
                    userId = p.UserId,
                    createdDate = p.CreatedDate.HasValue ? p.CreatedDate.Value.ToString("yyyy-MM-dd hh:mm tt") : "-"
                }),
                totalRecords = total,
                pageNo,
                hasNext,
                message = string.IsNullOrEmpty(err) ? "Success" : "DB Error"
            });
        }

        [HttpPost]
        [Route("get-product-by-id")]
        public async Task<IActionResult> GetProductById([FromBody] GetProductByIdReq model)
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            GetProductRes? response = null;
            int TotalRecords = 0;

            try
            {
                model ??= new GetProductByIdReq();
                model.UserId ??= Utility.Helper.GetUserId(User);
                (response, TotalRecords, StrDBError) = await _productService.GetProductById(model);

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

                string path = string.Format("\\Product\\GetProductById\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("add-product")]
        public async Task<IActionResult> AddProduct([FromBody] SaveProductReq model)
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
                model.ProductId = 0; // ensure insert
                model.UserId ??= Utility.Helper.GetUserId(User);
                (success, TotalRecords, StrDBError) = await _productService.SaveProduct(model);

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

                string path = string.Format("\\Product\\AddProduct\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("update-product")]
        public async Task<IActionResult> UpdateProduct([FromBody] SaveProductReq model)
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
                (success, TotalRecords, StrDBError) = await _productService.SaveProduct(model);

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

                string path = string.Format("\\Product\\UpdateProduct\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("product_status_update")]
        [Route("toggle-status")]
        [Route("toggle-status/{id}")]
        [Route("ToggleStatus/{id}")]
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
                (response, TotalRecords, StrDBError) = await _productService.ToggleStatus(model);

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

                string path = string.Format("\\Product\\ToggleStatus\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }

        [HttpPost]
        [Route("product_detail_create")]
        [Route("save-form")]
        public async Task<IActionResult> SaveForm([FromForm] Product product)
        {
            var (imgValid, imgErr) = FileHelper.ValidateImage(product.ImageFile, isRequired: true);
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
            if (product.ImageFile != null && product.ImageFile.Length > 0)
            {
                savedImageUrl = await FileHelper.SaveImageAsync(product.ImageFile, _env, "products");
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var saveReq = new SaveProductReq
            {
                ProductId = 0,
                Name = product.Name,
                Image = savedImageUrl,
                ImageUrl = savedImageUrl,
                BrandId = product.BrandId,
                CategoryId = product.CategoryId,
                Price = product.Price,
                MRP = product.MRP,
                SellingPrice = product.SellingPrice ?? product.Price,
                Size = product.Size,
                RAM = product.RAM,
                Storage = product.Storage,
                Color = product.Color,
                SKU = product.SKU,
                Barcode = product.Barcode,
                WarrantyMonths = product.WarrantyMonths > 0 ? product.WarrantyMonths : 12,
                TrackIMEI = product.TrackIMEI,
                IsActive = product.IsActive,
                UserId = currentUserId
            };

            var (success, _, err) = await _productService.SaveProduct(saveReq);
            if (success)
            {
                return StatusCode(201, new { success = true, message = "Product added successfully.", redirectUrl = "/Product" });
            }

            if (!string.IsNullOrEmpty(savedImageUrl))
            {
                FileHelper.DeleteImage(savedImageUrl, _env);
            }

            return BadRequest(new { success = false, message = "Unable to save product. Please check the entered details and try again." });
        }

        [HttpPut]
        [HttpPost]
        [Route("product_detail_update")]
        [Route("update-form")]
        [Route("update-form/{id:int}")]
        public async Task<IActionResult> UpdateForm([FromForm] Product product, [FromQuery(Name = "id")] int? queryId = null, int id = 0)
        {
            int explicitId = (queryId ?? 0) > 0 ? queryId!.Value : id;
            int targetId = explicitId > 0 ? explicitId : product.ProductId;
            if (targetId <= 0 || (explicitId > 0 && product.ProductId > 0 && explicitId != product.ProductId))
            {
                return BadRequest(new { success = false, message = "Invalid product ID." });
            }

            product.ProductId = targetId;
            int? currentUserId = Utility.Helper.GetUserId(User);
            var (existing, _, _) = await _productService.GetProductById(new GetProductByIdReq { ProductId = targetId, UserId = currentUserId });
            if (existing == null)
            {
                return NotFound(new { success = false, message = "Product not found." });
            }

            string? existingImg = existing.Image ?? existing.ImageUrl;
            bool hasExisting = !string.IsNullOrEmpty(existingImg);
            var (imgValid, imgErr) = FileHelper.ValidateImage(product.ImageFile, isRequired: !hasExisting);
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

            string finalImageUrl = existingImg ?? string.Empty;
            if (product.ImageFile != null && product.ImageFile.Length > 0)
            {
                finalImageUrl = await FileHelper.SaveImageAsync(product.ImageFile, _env, "products");
                FileHelper.DeleteImage(existingImg, _env);
            }

            var updateReq = new SaveProductReq
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Image = finalImageUrl,
                ImageUrl = finalImageUrl,
                BrandId = product.BrandId > 0 ? product.BrandId : existing.BrandId,
                CategoryId = product.CategoryId > 0 ? product.CategoryId : existing.CategoryId,
                Price = product.Price,
                MRP = product.MRP,
                SellingPrice = product.SellingPrice ?? product.Price,
                Size = product.Size,
                RAM = product.RAM,
                Storage = product.Storage,
                Color = product.Color,
                SKU = product.SKU,
                Barcode = product.Barcode,
                WarrantyMonths = product.WarrantyMonths > 0 ? product.WarrantyMonths : 12,
                TrackIMEI = product.TrackIMEI,
                IsActive = product.IsActive,
                UserId = currentUserId
            };

            var (success, _, _) = await _productService.SaveProduct(updateReq);
            if (success)
            {
                return Ok(new { success = true, message = "Product updated successfully.", redirectUrl = "/Product" });
            }

            return BadRequest(new { success = false, message = "Unable to update product. Please check the entered details and try again." });
        }

        [HttpDelete]
        [HttpPost]
        [Route("product_delete")]
        [Route("{id:int}")]
        [Route("delete-product")]
        [Route("delete-product/{id}")]
        [Route("Delete/{id}")]
        public async Task<IActionResult> DeleteProduct([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] DeleteReq? model, [FromQuery(Name = "id")] int? queryId = null, int? id = null)
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
                (response, TotalRecords, StrDBError) = await _productService.DeleteProduct(model);

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

                string path = string.Format("\\Product\\DeleteProduct\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }
    }
}
