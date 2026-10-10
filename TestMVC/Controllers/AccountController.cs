using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginReq());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginReq model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (res, _, err) = await _authService.LoginAsync(model);

            if (!res.Success)
            {
                if (!string.IsNullOrEmpty(err))
                {
                    ModelState.AddModelError(string.Empty, "System/Database error: " + err);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, res.Message);
                }
                return View(model);
            }

            // Create Authentication Cookie Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, res.UserId.ToString()),
                new Claim(ClaimTypes.Name, res.Username),
                new Claim(ClaimTypes.MobilePhone, res.MobileNumber),
                new Claim("IsApproved", res.IsApproved.ToString())
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

            TempData["Success"] = $"Welcome back, {res.Username}!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterReq());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterReq model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (res, _, err) = await _authService.RegisterAsync(model);

            if (!res.Success)
            {
                if (!string.IsNullOrEmpty(err))
                {
                    ModelState.AddModelError(string.Empty, "System/Database error: " + err);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, res.Message);
                }
                return View(model);
            }

            TempData["PendingApproval"] = res.Message;
            return RedirectToAction(nameof(Login));
        }

        // GET or POST: /Account/Logout
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/Users (User Approval Admin Management)
        [HttpGet]
        public async Task<IActionResult> Users(string? search, bool? isApproved)
        {
            var req = new GetUserReq
            {
                SearchText = search,
                IsApproved = isApproved,
                PageSize = 100
            };

            var (users, total, _) = await _authService.GetUsersAsync(req);
            ViewBag.Search = search;
            ViewBag.IsApproved = isApproved;
            ViewBag.Total = total;

            return View(users);
        }

        // POST: /Account/ToggleApproval/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            var (success, _, _) = await _authService.ToggleUserApprovalAsync(new ToggleApprovalReq { UserId = id });

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = success, message = success ? "User approval updated." : "Failed to update." });
            }

            TempData["Success"] = "User approval status updated.";
            return RedirectToAction(nameof(Users));
        }
    }
}
