using System.ComponentModel.DataAnnotations;

namespace Sycota.Web.Models.ViewModels;

public class ContactMessageViewModel
{
    [Required(ErrorMessage = "Името е задължително.")]
    [StringLength(100, ErrorMessage = "Името трябва да е до 100 символа.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Имейлът е задължителен.")]
    [EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Темата е задължителна.")]
    [StringLength(150, ErrorMessage = "Темата трябва да е до 150 символа.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Съобщението е задължително.")]
    [StringLength(5000, ErrorMessage = "Съобщението трябва да е до 5000 символа.")]
    public string Message { get; set; } = string.Empty;
}
