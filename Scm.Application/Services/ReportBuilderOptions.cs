using System.Collections.Generic;

namespace Scm.Application.Services;

public sealed class ReportBuilderOptions
{
    public List<string> HiddenSchemas { get; set; } = new()
    {
        "identity"
    };

    public List<string> HiddenTables { get; set; } = new()
    {
        "__EFMigrationsHistory"
    };

    public List<string> HiddenTablePrefixes { get; set; } = new()
    {
        "AspNet"
    };
}
