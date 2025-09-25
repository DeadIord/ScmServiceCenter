using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Scm.Infrastructure.Persistence;

public class ScmDbContextFactory : IDesignTimeDbContextFactory<ScmDbContext>
{
    public ScmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ScmDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=scmdb;Username=postgres;Password=postgres");

        return new ScmDbContext(optionsBuilder.Options);
    }
}
