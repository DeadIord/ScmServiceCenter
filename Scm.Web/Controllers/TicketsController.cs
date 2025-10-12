using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scm.Web.Authorization;

namespace Scm.Web.Controllers;

[Authorize(Policy = PolicyNames.CrmAccess)]
public sealed class TicketsController : Controller
{
    private readonly ILogger<TicketsController> m_logger;

    public TicketsController(ILogger<TicketsController> in_logger)
    {
        m_logger = in_logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        m_logger.LogInformation("Открыт раздел тикетов.");
        IActionResult ret = View();
        return ret;
    }
}
