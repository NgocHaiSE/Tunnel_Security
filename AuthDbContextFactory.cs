using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TunnelSecurity.Data.Auth;

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=TunnelSecurity;Username=postgres;Password=123456"
        );

        return new AuthDbContext(optionsBuilder.Options);
    }
}