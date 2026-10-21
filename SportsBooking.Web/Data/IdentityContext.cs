using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Data;

// Identity се пази в ОТДЕЛНА SQL Server база, за да не нарушава шестте оригинални таблици.
public class IdentityContext(DbContextOptions<IdentityContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>().Property(u => u.BookingUserId);
        // Обвързването е чрез ID; старите Users не се превръщат автоматично в акаунти.
    }
}
