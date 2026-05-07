#nullable disable

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Sycota.Domain.Entities;

namespace Sycota.Web.Areas.Identity.Pages.Account;

public class RegisterConfirmationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterConfirmationModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public string Email { get; set; }

    public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
    {
        if (email == null)
        {
            return RedirectToPage("/Index");
        }

        returnUrl ??= Url.Content("~/");
        Email = email;

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound($"Не може да се зареди потребител с имейл '{email}'.");
        }

        return Page();
    }
}
