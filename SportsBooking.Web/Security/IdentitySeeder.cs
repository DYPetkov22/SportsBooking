using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Security;

public static class IdentitySeeder
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, bool seedDemoAccounts)
    {
        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var business = scope.ServiceProvider.GetRequiredService<SportsBookingContext>();

        foreach (var name in new[] { AppRoles.Admin, AppRoles.Manager, AppRoles.User })
        {
            if (await roles.RoleExistsAsync(name)) continue;
            var result = await roles.CreateAsync(new IdentityRole(name));
            if (!result.Succeeded) throw new InvalidOperationException("Cannot create role " + name);
        }

        // Static demonstration credentials are created only when running in Development
        // AND explicitly enabled in the Development settings. Never in production.
        if (seedDemoAccounts)
        {
            await CreateIfMissingAsync(users, business, DemoAccounts.AdminEmail,
                DemoAccounts.AdminPassword, AppRoles.Admin, "Демо администратор");
            await CreateIfMissingAsync(users, business, DemoAccounts.ManagerEmail,
                DemoAccounts.ManagerPassword, AppRoles.Manager, "Демо мениджър");
        }

        // Optional secure bootstrap for an individually configured administrator account.
        var email = config["SPORTSBOOKING_ADMIN_EMAIL"]?.Trim();
        var password = config["SPORTSBOOKING_ADMIN_PASSWORD"];
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
            await CreateIfMissingAsync(users, business, email, password, AppRoles.Admin, "Администратор");
    }

    private static async Task CreateIfMissingAsync(UserManager<ApplicationUser> users,
        SportsBookingContext db, string email, string password, string role, string displayName)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing != null)
        {
            // Don't silently promote a user who registered this email before bootstrap.
            if (!await users.IsInRoleAsync(existing, role))
                throw new InvalidOperationException($"Account '{email}' exists without its expected role. Choose another address or investigate manually.");
            return; // Idempotent: don't overwrite changed passwords or existing data.
        }
        if (await db.Users.AnyAsync(p => p.Email == email))
            throw new InvalidOperationException($"Business profile email '{email}' already exists. Cannot assign elevated permissions.");

        var profile = new User { FullName = displayName, Email = email };
        db.Users.Add(profile);
        await db.SaveChangesAsync();
        var account = new ApplicationUser
        {
            UserName = email, Email = email, EmailConfirmed = true, BookingUserId = profile.UserID
        };
        try
        {
            var created = await users.CreateAsync(account, password);
            if (!created.Succeeded)
                throw new InvalidOperationException("Cannot seed " + role + ": " +
                    string.Join("; ", created.Errors.Select(x => x.Description)));
            var assigned = await users.AddToRoleAsync(account, role);
            if (!assigned.Succeeded)
            {
                await users.DeleteAsync(account);
                throw new InvalidOperationException("Cannot assign " + role + " role.");
            }
        }
        catch
        {
            db.Users.Remove(profile);
            await db.SaveChangesAsync();
            throw;
        }
    }
}
