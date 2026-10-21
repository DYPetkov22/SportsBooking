using System.ComponentModel.DataAnnotations;
namespace SportsBooking.Web.ViewModels;
public class RegisterViewModel
{
    [Required(ErrorMessage = "Въведете име.")]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = "";
    [RegularExpression(@"^\+?[0-9 ]{8,20}$", ErrorMessage = "Телефонът трябва да съдържа 8–20 цифри.")]
    public string? Phone { get; set; }
    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 10)]
    public string Password { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Паролите не съвпадат.")]
    public string ConfirmPassword { get; set; } = "";
}
