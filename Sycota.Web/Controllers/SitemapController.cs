using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml.Linq;

namespace Sycota.Web.Controllers;

/// <summary>
/// Controller for generating SEO sitemap.xml
/// </summary>
public class SitemapController : Controller
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SitemapController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour
    public IActionResult Index()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = $"{request?.Scheme}://{request?.Host}";

        var urls = new List<SitemapUrl>
        {
            new SitemapUrl { Loc = baseUrl, Priority = "1.0", ChangeFreq = "daily" },
            new SitemapUrl { Loc = $"{baseUrl}/Home/Privacy", Priority = "0.3", ChangeFreq = "monthly" },
            new SitemapUrl { Loc = $"{baseUrl}/Identity/Account/Login", Priority = "0.8", ChangeFreq = "monthly" },
            new SitemapUrl { Loc = $"{baseUrl}/Identity/Account/Register", Priority = "0.8", ChangeFreq = "monthly" },
        };

        var sitemap = GenerateSitemapXml(urls);
        return Content(sitemap, "application/xml", Encoding.UTF8);
    }

    private static string GenerateSitemapXml(List<SitemapUrl> urls)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var sitemap = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "urlset",
                urls.Select(url => new XElement(ns + "url",
                    new XElement(ns + "loc", url.Loc),
                    new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", url.ChangeFreq),
                    new XElement(ns + "priority", url.Priority)
                ))
            )
        );

        return sitemap.Declaration + Environment.NewLine + sitemap.ToString();
    }

    private class SitemapUrl
    {
        public string Loc { get; set; } = string.Empty;
        public string Priority { get; set; } = "0.5";
        public string ChangeFreq { get; set; } = "weekly";
    }
}
