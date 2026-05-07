#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Sycota.Domain.Entities;

namespace Sycota.Web.Areas.Identity.Pages.Account;

public class ResendEmailConfirmationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

    public ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Имейлът е задължителен.")]
        [EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
        public string Email { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user != null)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code },
                protocol: Request.Scheme);

            try
            {
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Потвърдете имейла си",
                    $"Моля, потвърдете акаунта си като <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>кликнете тук</a>.");
            }
            catch
            {
                TempData["Error"] = "Възникна проблем при изпращането на имейла за потвърждение.";
                return RedirectToPage("./Login");
            }
        }

        TempData["Success"] = "Ако има акаунт с този имейл, изпратихме нов линк за потвърждение.";
        return RedirectToPage("./Login");
    }
}
