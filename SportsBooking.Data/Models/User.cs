using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SportsBooking.Web.Models;

public class User
{
    [Key]
    public int UserID { get; set; }

    [Required(ErrorMessage = "Въведете име.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Името трябва да е от 2 до 100 символа.")]
    [Display(Name = "Име и фамилия")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Въведете имейл.")]
    [EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
    [StringLength(100)]
    [Display(Name = "Имейл")]
    public string Email { get; set; } = "";

    [RegularExpression(@"^\+?[0-9 ]{8,20}$", ErrorMessage = "Телефонът трябва да съдържа 8–20 цифри/интервали (може и +).")]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }

    [ValidateNever]
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
