using System.Globalization;
using SportsBooking.Web.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;
using SportsBooking.Web.Security;
using SportsBooking.Web.Services;
using SportsBooking.Web.Reporting;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var logDirectory = builder.Configuration["FileLogging:Directory"] ?? "App_Data/Logs";
if (!Path.IsPathRooted(logDirectory)) logDirectory = Path.Combine(builder.Environment.ContentRootPath, logDirectory);
builder.Logging.AddProvider(new FileErrorLoggerProvider(logDirectory));

builder.Services.AddControllersWithViews(o =>
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddSingleton<LocalFileStorage>();
builder.Services.AddScoped<ReportsService>();
builder.Services.AddScoped<SportsBooking.Data.IFieldCatalog, SportsBooking.Data.FieldCatalog>();
builder.Services.AddScoped<IPaymentStore, EfPaymentStore>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<IBookingAvailabilityStore, EfBookingAvailabilityStore>();
builder.Services.AddScoped<BookingAvailabilityService>();
// This university demo qualifies for the Community license. Check your license before commercial/public deployment.
QuestPDF.Settings.License = LicenseType.Community;

// SQL Server is the only provider. Development and Production use the same schema.
builder.Services.AddDbContext<SportsBookingContext>(options =>
    DatabaseSetup.Configure(options, builder.Configuration, "SportsBooking"));
builder.Services.AddDbContext<IdentityContext>(options =>
    DatabaseSetup.Configure(options, builder.Configuration, "Identity"));

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
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseStatusCodePagesWithReExecute("/Errors/{0}");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});
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

// Business schema is created with database/01_SportsBooking_SQLServer.sql.
// Identity uses a separate, newly created SQL Server database (not SQLite).
// EnsureCreated does not upgrade an existing schema: see docs/SQL_SERVER_SETUP_BG.md.
try
{
    using (var scope = app.Services.CreateScope())
    {
        var businessDb = scope.ServiceProvider.GetRequiredService<SportsBookingContext>();
        if (!await businessDb.Database.CanConnectAsync())
            throw new InvalidOperationException("Business SQL Server database is unavailable. Run database/01_SportsBooking_SQLServer.sql first.");

        await scope.ServiceProvider.GetRequiredService<IdentityContext>().Database.EnsureCreatedAsync();
    }
    await IdentitySeeder.InitializeAsync(app.Services, app.Configuration,
        app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DemoAccounts:Enabled"));

}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Database initialization failed");
    throw;
}

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
