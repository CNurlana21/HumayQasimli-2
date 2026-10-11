using System.ComponentModel.DataAnnotations;

namespace HumayQasimli.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad soyad daxil edin")]
        [Display(Name = "Ad Soyad")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifrə minimum 8 simvol olmalıdır")]
        [Display(Name = "Şifrə")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifrələr uygun gəlmir")]
        [Display(Name = "Şifrəni təkrarla")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Şifrə")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Məni xatırla")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class VerifyOtpViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Təsdiq kodunu daxil edin")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Kod 6 rəqəmdən ibarət olmalıdır")]
        [Display(Name = "Təsdiq kodu")]
        public string Code { get; set; } = string.Empty;
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8)]
        [Display(Name = "Yeni şifrə")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        [Display(Name = "Şifrəni təkrarla")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

   
}

