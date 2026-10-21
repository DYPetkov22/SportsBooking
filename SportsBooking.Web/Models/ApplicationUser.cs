using Microsoft.AspNetCore.Identity;

namespace SportsBooking.Web.Models;

// Отделен вход от таблицата Users. BookingUserId сочи към профила за резервации.
public class ApplicationUser : IdentityUser
{
    public int? BookingUserId { get; set; }
}
