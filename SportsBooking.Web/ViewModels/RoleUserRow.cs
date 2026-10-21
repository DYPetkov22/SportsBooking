namespace SportsBooking.Web.ViewModels;
public class RoleUserRow
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string CurrentRole { get; set; } = "";
    public bool IsCurrentUser { get; set; }
}
