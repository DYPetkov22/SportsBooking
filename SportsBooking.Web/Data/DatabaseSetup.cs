using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Data;

public static class DatabaseSetup
{
    public static void Configure(DbContextOptionsBuilder options, IConfiguration configuration,
        IWebHostEnvironment environment, string connectionName, bool useSqlite)
    {
        var connection = configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"Missing connection string: {connectionName}.");
        if (!useSqlite)
        {
            options.UseSqlServer(connection);
            return;
        }

        var sqlite = new SqliteConnectionStringBuilder(connection);
        sqlite.DataSource = Path.GetFullPath(sqlite.DataSource, environment.ContentRootPath);
        Directory.CreateDirectory(Path.GetDirectoryName(sqlite.DataSource)!);
        options.UseSqlite(sqlite.ToString());
    }

    public static async Task InitializeDevelopmentAsync(SportsBookingContext db)
    {
        // Only seed a newly created local database; never reset existing data on startup.
        if (!await db.Database.EnsureCreatedAsync()) return;

        var football = new Sport { SportName = "Футбол" };
        var tennis = new Sport { SportName = "Тенис" };
        var basketball = new Sport { SportName = "Баскетбол" };
        var east = new SportsCenter { CenterName = "Спортен център Изток", City = "София" };
        var west = new SportsCenter { CenterName = "Спортен център Запад", City = "Пловдив" };
        db.Fields.AddRange(
            new Field { FieldName = "Футболно игрище 1", Center = east, Sport = football, PricePerHour = 40m },
            new Field { FieldName = "Тенис корт 1", Center = east, Sport = tennis, PricePerHour = 20m },
            new Field { FieldName = "Баскетболно игрище 1", Center = west, Sport = basketball, PricePerHour = 25m },
            new Field { FieldName = "Футболно игрище 2", Center = west, Sport = football, PricePerHour = 35m });
        await db.SaveChangesAsync();
    }
}
