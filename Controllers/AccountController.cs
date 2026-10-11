using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HumayQasimli.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IOtpService _otpService;
        private readonly IFileUploadService _fileUploadService;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOtpService otpService,
            IFileUploadService fileUploadService,
            ApplicationDbContext db,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _otpService = otpService;
            _fileUploadService = fileUploadService;
            _db = db;
            _logger = logger;
        }

        // ---------------------------------------------------------------
        // Register
        // ---------------------------------------------------------------
        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                if (existingUser.EmailConfirmed)
                {
                    ModelState.AddModelError(string.Empty, "Bu email artiq qeydiyyatdan kecib.");
                    return View(model);
                }

                // User exists but never verified - resend OTP instead of duplicating.
                await _otpService.GenerateAndSendOtpAsync(existingUser.Id, existingUser.Email!);
                return RedirectToAction(nameof(VerifyOtp), new { userId = existingUser.Id, email = existingUser.Email });
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "User");
            await _otpService.GenerateAndSendOtpAsync(user.Id, user.Email!);

            return RedirectToAction(nameof(VerifyOtp), new { userId = user.Id, email = user.Email });
        }

        // ---------------------------------------------------------------
        // OTP verification
        // ---------------------------------------------------------------
        [HttpGet]
        public IActionResult VerifyOtp(string userId, string email)
        {
            return View(new VerifyOtpViewModel { UserId = userId, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Istifadeci tapilmadi.");
                return View(model);
            }

            var result = await _otpService.VerifyOtpAsync(model.UserId, model.Code);

            switch (result)
            {
                case OtpVerifyResult.Success:
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    TempData["Success"] = "Email ugurla tesdiqlendi. Xos gelmisiniz!";
                    return RedirectToAction("Index", "Home");

                case OtpVerifyResult.Expired:
                    ModelState.AddModelError(string.Empty, "Kodun muddeti bitib. Yeni kod isteyin.");
                    break;
                case OtpVerifyResult.TooManyAttempts:
                    ModelState.AddModelError(string.Empty, "Cox sayda yalnis cehd. Yeni kod isteyin.");
                    break;
                case OtpVerifyResult.NotFound:
                    ModelState.AddModelError(string.Empty, "Aktiv kod tapilmadi. Yeni kod isteyin.");
                    break;
                default:
                    ModelState.AddModelError(string.Empty, "Kod yanlisdir.");
                    break;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string userId, string email)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _otpService.GenerateAndSendOtpAsync(userId, email);

            TempData[result == OtpGenerateResult.Sent ? "Success" : "Error"] =
                result == OtpGenerateResult.Sent
                    ? "Yeni kod gonderildi."
                    : "Bir az sonra yeniden cehd edin (limit).";

            return RedirectToAction(nameof(VerifyOtp), new { userId, email });
        }

        // ---------------------------------------------------------------
        // Login / Logout
        // ---------------------------------------------------------------
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email ve ya sifre yanlisdir.");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                await _otpService.GenerateAndSendOtpAsync(user.Id, user.Email!);
                return RedirectToAction(nameof(VerifyOtp), new { userId = user.Id, email = user.Email });
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Hesab muveqqeti bloklanib. Bir az sonra yeniden cehd edin.");
                return View(model);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Email ve ya sifre yanlisdir.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() => View();

        // ---------------------------------------------------------------
        // Google login
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["Error"] = "Google ile giris zamani xeta bas verdi.";
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["Error"] = "Xarici giris melumati tapilmadi.";
                return RedirectToAction(nameof(Login));
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);
            }

            var email = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Name) ?? email ?? "Istifadeci";

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Google hesabinizdan email elde edile bilmedi.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    EmailConfirmed = true,
                    IsExternalLogin = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    TempData["Error"] = "Hesab yaradila bilmedi.";
                    return RedirectToAction(nameof(Login));
                }

                await _userManager.AddToRoleAsync(user, "User");
            }

            await _userManager.AddLoginAsync(user, info);
            await _signInManager.SignInAsync(user, isPersistent: false);

            return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);
        }

       
    }
}
