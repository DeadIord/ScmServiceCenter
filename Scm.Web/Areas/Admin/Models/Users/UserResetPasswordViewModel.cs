using System.ComponentModel.DataAnnotations;

namespace Scm.Web.Areas.Admin.Models.Users;

public class UserResetPasswordViewModel
{
    [Required]
    [Display(Name = "User_Id")]
    public string Id { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "User_Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(64, MinimumLength = 6)]
    [Display(Name = "User_Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password")]
    [Display(Name = "User_ConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
