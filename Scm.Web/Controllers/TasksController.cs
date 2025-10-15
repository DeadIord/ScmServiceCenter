using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Scm.Web.Controllers;

[Authorize(Roles = "Admin,Manager,Technician")]
public sealed class TasksController : Controller
{
    private readonly ILogger<TasksController> m_logger;

    public TasksController(ILogger<TasksController> in_logger)
    {
        m_logger = in_logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        m_logger.LogInformation("Открыт раздел задач.");
        IActionResult ret = View();
        return ret;
    }
}
