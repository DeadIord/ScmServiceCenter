using System.ComponentModel.DataAnnotations;
namespace Scm.Web.Areas.Admin.Models;

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [Display(Name = "User_UserName")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation_Required")]
    [EmailAddress(ErrorMessage = "Validation_Email")]
    [Display(Name = "User_Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Validation_Phone")]
    [Display(Name = "User_Phone")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "User_DisplayName")]
    public string? DisplayName { get; set; }

    [Display(Name = "User_IsActive")]
    public bool IsActive { get; set; }

    [Display(Name = "Role_List")]
    public IList<UserRoleSelectionViewModel> Roles { get; set; } = new List<UserRoleSelectionViewModel>();
}
