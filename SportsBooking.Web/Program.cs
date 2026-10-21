using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;
using SportsBooking.Web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(o =>
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var databaseProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var useSqlite = databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);
if (!useSqlite && !databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Database:Provider must be Sqlite or SqlServer.");
if (useSqlite && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("SQLite automatic setup is only available in Development. Configure SQL Server for production.");

builder.Services.AddDbContext<SportsBookingContext>(options =>
    DatabaseSetup.Configure(options, builder.Configuration, builder.Environment, "SportsBooking", useSqlite));
builder.Services.AddDbContext<IdentityContext>(options =>
    DatabaseSetup.Configure(options, builder.Configuration, builder.Environment, "Identity", useSqlite));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.User.RequireUniqueEmail = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.SignIn.RequireConfirmedEmail = false; // Няма конфигурирана email услуга в учебния проект.
})
.AddEntityFrameworkStores<IdentityContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
var culture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new List<CultureInfo> { culture },
    SupportedUICultures = new List<CultureInfo> { culture }
});
app.UseRouting();
app.UseAuthentication(); // ТРЯБВА да е преди UseAuthorization.
app.UseAuthorization();

// Local SQLite databases initialize automatically. SQL Server business data uses the original SQL script.
using (var scope = app.Services.CreateScope())
{
    if (useSqlite)
        await DatabaseSetup.InitializeDevelopmentAsync(scope.ServiceProvider.GetRequiredService<SportsBookingContext>());
    await scope.ServiceProvider.GetRequiredService<IdentityContext>().Database.EnsureCreatedAsync();
}
await IdentitySeeder.InitializeAsync(app.Services, app.Configuration);

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
