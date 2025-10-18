namespace Scm.Web.Security;

public static class AuthorizationPolicies
{
    public const string StockAccess = "StockAccess";
    public const string ReportsAccess = "ReportsAccess";

    public static readonly string[] s_stockAccessRoles = { "Admin", "Storekeeper" };
    public static readonly string[] s_reportsAccessRoles = { "Admin", "Manager", "Accountant" };
}
