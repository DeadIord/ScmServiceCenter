using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Scm.Infrastructure.Identity;

namespace Scm.Web.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> m_signInManager;
    private readonly UserManager<ApplicationUser> m_userManager;
    private readonly ILogger<LoginModel> m_logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<LoginModel> logger)
    {
        m_signInManager = signInManager;
        m_userManager = userManager;
        m_logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Укажите электронную почту")]
        [EmailAddress(ErrorMessage = "Введите корректную почту")]
        [Display(Name = "Электронная почта")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        var targetReturnUrl = returnUrl ?? Url.Content("~/");

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ReturnUrl = targetReturnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var targetReturnUrl = returnUrl ?? Url.Content("~/");

        if (ModelState.IsValid)
        {
            var user = await m_userManager.FindByEmailAsync(Input.Email);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Неверные учетные данные.");
            }
            else
            {
                var signInResult = await m_signInManager.PasswordSignInAsync(user.UserName!, Input.Password, Input.RememberMe, false);
                if (signInResult.Succeeded)
                {
                    m_logger.LogInformation("Пользователь успешно вошел в систему.");
                    return LocalRedirect(targetReturnUrl);
                }

                if (signInResult.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Учетная запись временно заблокирована. Обратитесь к администратору.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Неверные учетные данные.");
                }
            }
        }

        ReturnUrl = targetReturnUrl;
        return Page();
    }
}
