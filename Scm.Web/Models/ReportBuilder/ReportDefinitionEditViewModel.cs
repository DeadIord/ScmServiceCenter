using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Scm.Domain.Entities;

namespace Scm.Web.Models.ReportBuilder;

public class ReportDefinitionEditViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "Название отчёта")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "SQL-запрос")]
    public string SqlText { get; set; } = string.Empty;

    [Display(Name = "Конфигурация конструктора")]
    public string BuilderConfigurationJson { get; set; } = "{}";

    [Display(Name = "Параметры")]
    public List<ReportParameterInputModel> Parameters { get; set; } = new();

    [Display(Name = "Доступность")]
    public ReportVisibility Visibility { get; set; } = ReportVisibility.Private;

    [Display(Name = "Разрешённые роли")]
    public List<string> AllowedRoles { get; set; } = new();

    public IEnumerable<SelectListItem> AvailableRoles { get; set; } = Array.Empty<SelectListItem>();

    public bool IsActive { get; set; } = true;

    [Display(Name = "Разрешить ручное редактирование SQL")]
    public bool IsManualSqlAllowed { get; set; }
        = false;
}
