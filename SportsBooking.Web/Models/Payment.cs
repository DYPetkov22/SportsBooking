using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SportsBooking.Web.Models;

public class Payment
{
    [Key]
    public int PaymentID { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Изберете резервация.")]
    [Display(Name = "Резервация")]
    public int BookingID { get; set; }

    [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "Сумата трябва да е положителна.")]
    [Display(Name = "Сума (EUR)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Изберете начин на плащане.")]
    [RegularExpression("^(Карта|В брой)$", ErrorMessage = "Невалиден начин на плащане.")]
    [Display(Name = "Начин на плащане")]
    public string PaymentMethod { get; set; } = "Карта";

    [ValidateNever]
    public Booking? Booking { get; set; }
}
