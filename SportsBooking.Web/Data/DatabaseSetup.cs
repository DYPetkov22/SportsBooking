using Microsoft.EntityFrameworkCore;

namespace SportsBooking.Web.Data;

public static class DatabaseSetup
{
    public static void Configure(DbContextOptionsBuilder options, IConfiguration configuration, string connectionName)
    {
        var connectionString = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Missing SQL Server connection string: {connectionName}.");

        // Keep the default execution strategy: booking creation uses an explicit
        // SERIALIZABLE transaction, which is incompatible with implicit retry strategies.
        options.UseSqlServer(connectionString);
    }
}
