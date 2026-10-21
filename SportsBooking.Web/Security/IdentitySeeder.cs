using Microsoft.AspNetCore.Identity;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Security;

public static class IdentitySeeder
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var name in new[] { AppRoles.Admin, AppRoles.Manager, AppRoles.User })
        {
            if (!await roles.RoleExistsAsync(name))
            {
                var result = await roles.CreateAsync(new IdentityRole(name));
                if (!result.Succeeded) throw new InvalidOperationException("Cannot seed role: " + name);
            }
        }

        // Секретите НЕ се пазят в source control/appsettings.json.
        // Задайте двата environment variable преди ПЪРВОТО пускане.
        var email = config["SPORTSBOOKING_ADMIN_EMAIL"]?.Trim();
        var password = config["SPORTSBOOKING_ADMIN_PASSWORD"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        var admin = await users.FindByEmailAsync(email);
        if (admin == null)
        {
            admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await users.CreateAsync(admin, password);
            if (!result.Succeeded)
                throw new InvalidOperationException("Admin account cannot be created: " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        // Не повишаваме вече съществуващ акаунт автоматично.
        // Това предпазва от предварителна регистрация с бъдещ администраторски имейл.
        else if (!await users.IsInRoleAsync(admin, AppRoles.Admin))
            throw new InvalidOperationException("Admin email is already registered. Choose a new admin email.");

        if (!await users.IsInRoleAsync(admin, AppRoles.Admin))
        {
            var assigned = await users.AddToRoleAsync(admin, AppRoles.Admin);
            if (!assigned.Succeeded) throw new InvalidOperationException("Cannot grant admin role.");
        }
    }
}
