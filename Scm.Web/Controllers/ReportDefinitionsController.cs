using System.IO;
using System.Linq;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Infrastructure.Identity;
using Scm.Infrastructure.Persistence;
using Scm.Web.Models.ReportBuilder;

namespace Scm.Web.Controllers;

[Authorize]
public class ReportDefinitionsController : Controller
{
    private readonly ScmDbContext m_dbContext;
    private readonly IReportBuilderService m_reportBuilderService;
    private readonly IReportMetadataService m_reportMetadataService;
    private readonly UserManager<ApplicationUser> m_userManager;
    private readonly RoleManager<IdentityRole> m_roleManager;
    private readonly ILogger<ReportDefinitionsController> m_logger;

    public ReportDefinitionsController(
        ScmDbContext in_dbContext,
        IReportBuilderService in_reportBuilderService,
        IReportMetadataService in_reportMetadataService,
        UserManager<ApplicationUser> in_userManager,
        RoleManager<IdentityRole> in_roleManager,
        ILogger<ReportDefinitionsController> in_logger)
    {
        m_dbContext = in_dbContext;
        m_reportBuilderService = in_reportBuilderService;
        m_reportMetadataService = in_reportMetadataService;
        m_userManager = in_userManager;
        m_roleManager = in_roleManager;
        m_logger = in_logger;
    }

    [HttpGet]
    public async Task<IActionResult> Metadata(CancellationToken in_cancellationToken)
    {
        var metadata = await m_reportMetadataService.GetMetadataAsync(in_cancellationToken);
        return Json(metadata);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateSql([FromBody] ReportQueryRequest in_request, CancellationToken in_cancellationToken)
    {
        if (in_request is null)
        {
            return BadRequest(new { error = "Не передана конфигурация отчёта." });
        }

        try
        {
            var sqlResult = await m_reportBuilderService.BuildSqlAsync(in_request, in_cancellationToken);
            var metadata = await m_reportMetadataService.GetMetadataAsync(in_cancellationToken);
            var allowedSchemas = metadata.Schemas.Select(schema => schema.Name);
            m_reportBuilderService.ValidateSqlSafety(sqlResult.Sql, allowedSchemas);
            return Json(sqlResult);
        }
        catch (Exception ex)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Json(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUser = await m_userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var userRoles = await m_userManager.GetRolesAsync(currentUser);
        var reports = await m_dbContext.ReportDefinitions
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        var model = reports
            .Where(r => CanAccess(r, currentUser.Id, userRoles))
            .Select(r => new ReportDefinitionListItemViewModel
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Visibility = r.Visibility,
                CreatedAtUtc = r.CreatedAtUtc,
                CreatedBy = r.CreatedBy,
                CanEdit = CanEdit(r, currentUser.Id, userRoles)
            })
            .ToList();

        ViewData["Title"] = "Конструктор отчётов";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        var currentUser = await m_userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        ReportDefinition? report = null;
        if (id.HasValue)
        {
            report = await m_dbContext.ReportDefinitions.FindAsync(id.Value);
            if (report is null)
            {
                return NotFound();
            }

            var roles = await m_userManager.GetRolesAsync(currentUser);
            if (!CanEdit(report, currentUser.Id, roles))
            {
                return Forbid();
            }
        }

        var viewModel = await BuildEditModel(report);
        ViewData["Title"] = report is null ? "Новый отчёт" : $"Редактирование: {report.Title}";
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid? id, ReportDefinitionEditViewModel model)
    {
        var currentUser = await m_userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        ReportDefinition? report = null;
        if (id.HasValue)
        {
            report = await m_dbContext.ReportDefinitions.FirstOrDefaultAsync(r => r.Id == id.Value);
            if (report is null)
            {
                return NotFound();
            }

            var roles = await m_userManager.GetRolesAsync(currentUser);
            if (!CanEdit(report, currentUser.Id, roles))
            {
                return Forbid();
            }
        }

        model.Parameters = model.Parameters.Where(parameter => !string.IsNullOrWhiteSpace(parameter.Name)).ToList();
        if (!model.Parameters.Any())
        {
            model.Parameters.Add(new ReportParameterInputModel { Name = "", Type = "string" });
        }

        var configuration = m_reportBuilderService.DeserializeQuery(model.BuilderConfigurationJson);
        configuration.AllowManualSql = model.IsManualSqlAllowed;
        var configurationJson = m_reportBuilderService.SerializeQuery(configuration);
        model.BuilderConfigurationJson = configurationJson;

        ReportSqlGenerationResult? generatedSql = null;
        try
        {
            generatedSql = await m_reportBuilderService.BuildSqlAsync(configuration);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(nameof(model.BuilderConfigurationJson), ex.Message);
        }

        if (!model.IsManualSqlAllowed && generatedSql is not null)
        {
            model.SqlText = generatedSql.Sql;
        }

        var metadata = await m_reportMetadataService.GetMetadataAsync();
        var allowedSchemas = metadata.Schemas.Select(schema => schema.Name);
        try
        {
            m_reportBuilderService.ValidateSqlSafety(model.SqlText, allowedSchemas);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(nameof(model.SqlText), ex.Message);
        }

        if (!ModelState.IsValid)
        {
            await PopulateRoles(model);
            ViewData["Title"] = report is null ? "Новый отчёт" : $"Редактирование: {report?.Title}";
            return View(model);
        }

        var parameterDefinitions = model.Parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new ReportParameterDefinition
            {
                Name = NormalizeParameterName(p.Name),
                Type = p.Type,
                DefaultValue = p.DefaultValue
            })
            .ToList();

        var allowedRoles = model.AllowedRoles ?? new List<string>();
        var serializedParameters = m_reportBuilderService.SerializeParameters(parameterDefinitions);
        var serializedRoles = m_reportBuilderService.SerializeRoles(allowedRoles);

        if (report is null)
        {
            report = new ReportDefinition
            {
                Id = Guid.NewGuid(),
                Title = model.Title,
                Description = model.Description,
                SqlText = model.SqlText,
                BuilderConfigurationJson = configurationJson,
                ParametersJson = serializedParameters,
                Visibility = model.Visibility,
                AllowedRolesJson = serializedRoles,
                CreatedBy = currentUser.Id,
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = model.IsActive
            };

            m_dbContext.ReportDefinitions.Add(report);
        }
        else
        {
            report.Title = model.Title;
            report.Description = model.Description;
            report.SqlText = model.SqlText;
            report.BuilderConfigurationJson = configurationJson;
            report.ParametersJson = serializedParameters;
            report.Visibility = model.Visibility;
            report.AllowedRolesJson = serializedRoles;
            report.IsActive = model.IsActive;
            m_dbContext.ReportDefinitions.Update(report);
        }

        m_logger.LogInformation("Конфигурация отчёта {ReportId} обновлена пользователем {UserId}", report.Id, currentUser.Id);

        await m_dbContext.SaveChangesAsync();
        TempData["Success"] = "Отчёт успешно сохранён.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Preview(Guid id)
    {
        var currentUser = await m_userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var report = await m_dbContext.ReportDefinitions.FindAsync(id);
        if (report is null || !report.IsActive)
        {
            return NotFound();
        }

        var roles = await m_userManager.GetRolesAsync(currentUser);
        if (!CanAccess(report, currentUser.Id, roles))
        {
            return Forbid();
        }

        var parameters = m_reportBuilderService.DeserializeParameters(report.ParametersJson)
            .Select(p => new ReportParameterInputModel
            {
                Name = p.Name,
                Type = p.Type,
                DefaultValue = p.DefaultValue
            })
            .ToList();

        var model = new ReportPreviewViewModel
        {
            ReportId = report.Id,
            Title = report.Title,
            Description = report.Description,
            Parameters = parameters,
            Values = parameters.ToDictionary(p => p.Name, p => p.DefaultValue)
        };

        ViewData["Title"] = $"Предпросмотр: {report.Title}";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(Guid id, Dictionary<string, string?> values)
    {
        values ??= new Dictionary<string, string?>();

        var currentUser = await m_userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var report = await m_dbContext.ReportDefinitions.FindAsync(id);
        if (report is null || !report.IsActive)
        {
            return NotFound();
        }

        var roles = await m_userManager.GetRolesAsync(currentUser);
        if (!CanAccess(report, currentUser.Id, roles))
        {
            return Forbid();
        }

        var parameterDefinitions = m_reportBuilderService.DeserializeParameters(report.ParametersJson);
        var parameters = parameterDefinitions.Select(p => new ReportParameterInputModel
        {
            Name = p.Name,
            Type = p.Type,
            DefaultValue = p.DefaultValue
        }).ToList();

        var executionModel = new ReportPreviewViewModel
        {
            ReportId = report.Id,
            Title = report.Title,
            Description = report.Description,
            Parameters = parameters,
            Values = values
        };

        var log = new ReportExecutionLog
        {
            Id = Guid.NewGuid(),
            ReportId = report.Id,
            StartedAtUtc = DateTime.UtcNow,
            Status = ReportExecutionStatus.Success
        };

        try
        {
            var result = await m_reportBuilderService.ExecuteAsync(report, values, true);
            executionModel.Columns = result.Columns;
            executionModel.Rows = result.Rows;
            executionModel.Error = result.Error;
            log.RowCount = result.TotalRows;
            if (!string.IsNullOrEmpty(result.Error))
            {
                log.Status = ReportExecutionStatus.Fail;
                log.ErrorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            executionModel.Error = ex.Message;
            log.Status = ReportExecutionStatus.Fail;
            log.ErrorMessage = ex.Message;
        }
        finally
        {
            log.FinishedAtUtc = DateTime.UtcNow;
            m_dbContext.ReportExecutionLogs.Add(log);
            await m_dbContext.SaveChangesAsync();
        }

        ViewData["Title"] = $"Предпросмотр: {report.Title}";
        return View(executionModel);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportCsv(Guid id, Dictionary<string, string?> values)
    {
        return await ExportInternal(id, values, "csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportXlsx(Guid id, Dictionary<string, string?> values)
    {
        return await ExportInternal(id, values, "xlsx");
    }

    private async Task<IActionResult> ExportInternal(Guid id, Dictionary<string, string?> values, string format)
    {
        values ??= new Dictionary<string, string?>();

        var currentUser = await m_userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var report = await m_dbContext.ReportDefinitions.FindAsync(id);
        if (report is null || !report.IsActive)
        {
            return NotFound();
        }

        var roles = await m_userManager.GetRolesAsync(currentUser);
        if (!CanAccess(report, currentUser.Id, roles))
        {
            return Forbid();
        }

        var log = new ReportExecutionLog
        {
            Id = Guid.NewGuid(),
            ReportId = report.Id,
            StartedAtUtc = DateTime.UtcNow,
            Status = ReportExecutionStatus.Success
        };

        try
        {
            var result = await m_reportBuilderService.ExecuteAsync(report, values, false);
            if (!string.IsNullOrEmpty(result.Error))
            {
                log.Status = ReportExecutionStatus.Fail;
                log.ErrorMessage = result.Error;
                log.RowCount = 0;
                return BadRequest(result.Error);
            }

            log.RowCount = result.TotalRows;
            return format == "csv"
                ? File(BuildCsv(result), "text/csv", $"{SanitizeFileName(report.Title)}.csv")
                : File(BuildXlsx(result), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{SanitizeFileName(report.Title)}.xlsx");
        }
        catch (Exception ex)
        {
            log.Status = ReportExecutionStatus.Fail;
            log.ErrorMessage = ex.Message;
            log.RowCount = 0;
            return BadRequest(ex.Message);
        }
        finally
        {
            log.FinishedAtUtc = DateTime.UtcNow;
            m_dbContext.ReportExecutionLogs.Add(log);
            await m_dbContext.SaveChangesAsync();
        }
    }

    private async Task<ReportDefinitionEditViewModel> BuildEditModel(ReportDefinition? report)
    {
        var configuration = report is null
            ? new ReportQueryRequest()
            : m_reportBuilderService.DeserializeQuery(report.BuilderConfigurationJson);

        var model = new ReportDefinitionEditViewModel
        {
            Id = report?.Id,
            Title = report?.Title ?? string.Empty,
            Description = report?.Description,
            SqlText = report?.SqlText ?? string.Empty,
            Visibility = report?.Visibility ?? ReportVisibility.Private,
            IsActive = report?.IsActive ?? true,
            BuilderConfigurationJson = m_reportBuilderService.SerializeQuery(configuration),
            IsManualSqlAllowed = configuration.AllowManualSql
        };

        if (report is not null)
        {
            var parameters = m_reportBuilderService.DeserializeParameters(report.ParametersJson);
            model.Parameters = parameters.Select(p => new ReportParameterInputModel
            {
                Name = p.Name,
                Type = p.Type,
                DefaultValue = p.DefaultValue
            }).ToList();
            if (!model.Parameters.Any())
            {
                model.Parameters.Add(new ReportParameterInputModel());
            }

            var roles = m_reportBuilderService.DeserializeRoles(report.AllowedRolesJson);
            model.AllowedRoles = roles.ToList();
        }
        else
        {
            model.Parameters.Add(new ReportParameterInputModel());
        }

        await PopulateRoles(model);
        return model;
    }

    private async Task PopulateRoles(ReportDefinitionEditViewModel model)
    {
        var allRoles = await m_roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        model.AllowedRoles ??= new List<string>();

        model.AvailableRoles = allRoles.Select(r =>
        {
            var roleName = r.Name ?? string.Empty;
            return new SelectListItem
            {
                Text = roleName,
                Value = roleName,
                Selected = model.AllowedRoles.Contains(roleName)
            };
        });
    }

    private static string NormalizeParameterName(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        return trimmed.StartsWith("@", StringComparison.Ordinal) ? trimmed : "@" + trimmed;
    }

    private bool CanAccess(ReportDefinition report, string userId, IEnumerable<string> roles)
    {
        if (report.CreatedBy == userId)
        {
            return true;
        }

        var allowedRoles = m_reportBuilderService.DeserializeRoles(report.AllowedRolesJson);
        return report.Visibility switch
        {
            ReportVisibility.Private => false,
            ReportVisibility.Team => allowedRoles.Any(role => roles.Contains(role, StringComparer.OrdinalIgnoreCase)),
            ReportVisibility.Organization => !allowedRoles.Any() || allowedRoles.Any(role => roles.Contains(role, StringComparer.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private bool CanEdit(ReportDefinition report, string userId, IEnumerable<string> roles)
    {
        if (report.CreatedBy == userId)
        {
            return true;
        }

        return roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) || string.Equals(r, "Manager", StringComparison.OrdinalIgnoreCase));
    }

    private static byte[] BuildCsv(ReportExecutionResult result)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);

        writer.WriteLine(string.Join(';', result.Columns.Select(c => EscapeCsv(c))));
        foreach (var row in result.Rows)
        {
            var values = result.Columns.Select(column => EscapeCsv(row.TryGetValue(column, out var value) ? value : null));
            writer.WriteLine(string.Join(';', values));
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] BuildXlsx(ReportExecutionResult result)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Отчёт");

        for (var i = 0; i < result.Columns.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = result.Columns[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        for (var rowIndex = 0; rowIndex < result.Rows.Count; rowIndex++)
        {
            var row = result.Rows[rowIndex];
            for (var columnIndex = 0; columnIndex < result.Columns.Count; columnIndex++)
            {
                var columnName = result.Columns[columnIndex];
                row.TryGetValue(columnName, out var value);
                worksheet.Cell(rowIndex + 2, columnIndex + 1).SetValue(value?.ToString() ?? string.Empty);
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string EscapeCsv(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        if (text.Contains('"') || text.Contains(';') || text.Contains('\n'))
        {
            text = "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        return text;
    }

    private static string SanitizeFileName(string title)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = string.Join('_', title.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(cleaned) ? "report" : cleaned;
    }
}
