using Microsoft.EntityFrameworkCore;
using SportsBooking.Data;
using SportsBooking.Web.Data;
using SportsBooking.Api;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("SportsBooking")
    ?? throw new InvalidOperationException("Missing SportsBooking connection string.");
builder.Services.AddDbContext<SportsBookingContext>(options => options.UseSqlServer(connection));
builder.Services.AddScoped<IFieldCatalog, FieldCatalog>();
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapFieldCatalog();
app.Run();
