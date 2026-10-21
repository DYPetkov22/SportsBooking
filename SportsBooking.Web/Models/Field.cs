using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SportsBooking.Web.Models;

public class Field
{
    [Key]
    public int FieldID { get; set; }

    [Required(ErrorMessage = "Въведете име на игрището.")]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Игрище")]
    public string FieldName { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Изберете спортен център.")]
    [Display(Name = "Спортен център")]
    public int CenterID { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Изберете спорт.")]
    [Display(Name = "Спорт")]
    public int SportID { get; set; }

    [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "Цената трябва да е положителна.")]
    [Display(Name = "Цена за час (EUR)")]
    public decimal PricePerHour { get; set; }

    [ValidateNever]
    public SportsCenter? Center { get; set; }

    [ValidateNever]
    public Sport? Sport { get; set; }

    [ValidateNever]
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
