using System.ComponentModel.DataAnnotations;

namespace Scm.Web.Models.ReportBuilder;

public class ReportParameterInputModel
{
    [Required]
    [Display(Name = "Имя параметра")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Тип")]
    public string Type { get; set; } = "string";

    [Display(Name = "Значение по умолчанию")]
    public string? DefaultValue { get; set; }
}
