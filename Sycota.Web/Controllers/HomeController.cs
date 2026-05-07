using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using Sycota.Web.Models;
using Sycota.Web.Models.ViewModels;
using System.Diagnostics;

namespace Sycota.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly EmailSettings _emailSettings;

        public HomeController(ILogger<HomeController> logger, IEmailSender emailSender, IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _emailSender = emailSender;
            _emailSettings = emailSettings.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Features()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View(new ContactMessageViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactMessageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(_emailSettings.ContactRecipientEmail))
            {
                ModelState.AddModelError(string.Empty, "Няма конфигуриран имейл за получаване на съобщения.");
                return View(model);
            }

            var subject = $"[SYCOTA+] Контакт: {model.Subject}";
            var body = $@"
                <p><strong>Име:</strong> {System.Net.WebUtility.HtmlEncode(model.Name)}</p>
                <p><strong>Имейл:</strong> {System.Net.WebUtility.HtmlEncode(model.Email)}</p>
                <p><strong>Тема:</strong> {System.Net.WebUtility.HtmlEncode(model.Subject)}</p>
                <hr />
                <p>{System.Net.WebUtility.HtmlEncode(model.Message).Replace("\n", "<br />")}</p>";

            try
            {
                await _emailSender.SendEmailAsync(_emailSettings.ContactRecipientEmail, subject, body);
                TempData["Success"] = "Съобщението е изпратено успешно.";
                return RedirectToAction(nameof(Contact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact form email.");
                ModelState.AddModelError(string.Empty, "Възникна проблем при изпращането. Моля, опитайте отново по-късно.");
                return View(model);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
