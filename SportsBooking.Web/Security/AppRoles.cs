namespace SportsBooking.Web.Security;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";
    public const string Staff = Admin + "," + Manager;
}
