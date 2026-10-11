using System.ComponentModel.DataAnnotations;

namespace HumayQasimli.Models.ViewModels
{
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Ad daxil edin")]
        [StringLength(150)]
        [Display(Name = "Adiniz")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "Movzu")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Mesajinizi yazin")]
        [StringLength(4000)]
        [Display(Name = "Mesaj")]
        public string Message { get; set; } = string.Empty;
    }

  
}
