using Microsoft.AspNetCore.Mvc;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Username and password are required");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View();
                }

                var user = await _authService.AuthenticateAsync(username, password);
                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid username or password");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View();
                }

                // Ensure user has required data
                if (user.Role != "InvoiceManager" && !user.EmployeeId.HasValue)
                {
                    ModelState.AddModelError("", "Account is not properly configured. Please contact support.");
                    _logger.LogWarning("User {Username} attempted to login but has no EmployeeId", username);
                    return View();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("UserId", user.Id.ToString())
                };

                if (user.EmployeeId.HasValue)
                {
                    claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User {Username} logged in successfully", username);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", username);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
} 