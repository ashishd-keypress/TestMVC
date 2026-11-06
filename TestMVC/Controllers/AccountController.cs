using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;

namespace TestMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            // If the user is already authenticated:
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // If there's a valid local return URL that is NOT a login/logout loop, redirect there
                if (IsSafeRedirectUrl(returnUrl))
                {
                    return Redirect(returnUrl!);
                }

                // If returnUrl points back to Login/Logout or an access denied bounce, clear stale/corrupt cookies
                // to completely eliminate any redirect loop (ERR_TOO_MANY_REDIRECTS)
                if (!string.IsNullOrEmpty(returnUrl) && !returnUrl.Equals("/", StringComparison.OrdinalIgnoreCase))
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    Response.Cookies.Delete("TestMVC.Auth");
                    Response.Cookies.Delete(".AspNetCore.Cookies");
                    Response.Cookies.Delete(CookieAuthenticationDefaults.AuthenticationScheme);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.ReturnUrl = returnUrl;
            string code = GenerateCaptchaCode();
            var model = new LoginReq
            {
                ExpectedValidationCode = code
            };
            ViewBag.ValidationCode = code;

            return View(model);
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginReq model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            // 1. Verify Validation Code
            if (string.IsNullOrWhiteSpace(model.ValidationCode) ||
                string.IsNullOrWhiteSpace(model.ExpectedValidationCode) ||
                !string.Equals(model.ValidationCode.Trim(), model.ExpectedValidationCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Invalid validation code. Please enter the 4-digit code shown.");
                RefreshCaptcha(model);
                return View(model);
            }

            // 2. Validate input fields
            if (!ModelState.IsValid)
            {
                RefreshCaptcha(model);
                return View(model);
            }

            // 3. Authenticate Credentials
            var (res, _, err) = await _authService.LoginAsync(model);

            if (!res.Success)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password. Please check your credentials.");
                RefreshCaptcha(model);
                return View(model);
            }

            // 4. Create Authentication Cookie Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, res.UserId.ToString()),
                new Claim(ClaimTypes.Name, res.Username),
                new Claim(ClaimTypes.MobilePhone, res.MobileNumber ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["Success"] = $"Welcome, {res.Username}!";

            if (IsSafeRedirectUrl(returnUrl))
            {
                return Redirect(returnUrl!);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/GetNewCaptcha
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetNewCaptcha()
        {
            string code = GenerateCaptchaCode();
            return Json(new { code });
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET or POST: /Account/Logout
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("TestMVC.Auth");
            Response.Cookies.Delete(".AspNetCore.Cookies");
            Response.Cookies.Delete(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "You have been logged out successfully.";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, redirectUrl = Url.Action("Login", "Account") });
            }

            return RedirectToAction(nameof(Login));
        }

        private bool IsSafeRedirectUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl)) return false;
            if (!Url.IsLocalUrl(returnUrl)) return false;

            // Reject URLs that loop back to Login, Logout, or AccessDenied
            if (returnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase)) return false;
            if (returnUrl.Contains("/Account/Logout", StringComparison.OrdinalIgnoreCase)) return false;
            if (returnUrl.Contains("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase)) return false;
            if (returnUrl.Trim().Equals("/", StringComparison.OrdinalIgnoreCase)) return false;

            return true;
        }

        private static string GenerateCaptchaCode()
        {
            return Random.Shared.Next(1000, 9999).ToString();
        }

        private void RefreshCaptcha(LoginReq model)
        {
            string newCode = GenerateCaptchaCode();
            model.ExpectedValidationCode = newCode;
            model.ValidationCode = string.Empty;
            ViewBag.ValidationCode = newCode;
        }
    }
}
