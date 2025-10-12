using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Scm.Web.Controllers
{
    public class CultureController : Controller
    {
        private readonly IOptions<RequestLocalizationOptions> m_localizationOptions;

        public CultureController(IOptions<RequestLocalizationOptions> in_localizationOptions)
        {
            m_localizationOptions = in_localizationOptions;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Set(string in_culture, string in_returnUrl)
        {
            string returnUrl = string.IsNullOrWhiteSpace(in_returnUrl) || !Url.IsLocalUrl(in_returnUrl)
                ? Url.Content("~/")
                : in_returnUrl;
            IActionResult ret = Redirect(returnUrl);

            if (!string.IsNullOrWhiteSpace(in_culture))
            {
                bool isCultureSupported = m_localizationOptions.Value.SupportedUICultures
                    .Any(culture => string.Equals(culture.Name, in_culture, StringComparison.OrdinalIgnoreCase));

                if (isCultureSupported)
                {
                    Response.Cookies.Append(
                        CookieRequestCultureProvider.DefaultCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(in_culture)),
                        new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddYears(1)
                        });
                }
            }

            return ret;
        }
    }
}
