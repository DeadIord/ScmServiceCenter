using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scm.Web.Authorization;

namespace Scm.Web.Controllers;

[Authorize(Policy = PolicyNames.CrmAccess)]
public sealed class DealsController : Controller
{
    private readonly ILogger<DealsController> m_logger;

    public DealsController(ILogger<DealsController> in_logger)
    {
        m_logger = in_logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        m_logger.LogInformation("Открыт раздел сделок.");
        IActionResult ret = View();
        return ret;
    }
}
