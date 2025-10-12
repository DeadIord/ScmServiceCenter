namespace Scm.Web.Areas.Admin.Models;

public class UserListViewModel
{
    public IList<UserListItemViewModel> Users { get; set; } = new List<UserListItemViewModel>();
}
