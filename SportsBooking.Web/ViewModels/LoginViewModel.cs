using System.ComponentModel.DataAnnotations;
namespace SportsBooking.Web.ViewModels;
public class LoginViewModel
{
    [Required(ErrorMessage = "Въведете имейл.")]
    [EmailAddress]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Въведете парола.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}
