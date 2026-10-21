using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SportsBooking.Web.Models;

public class Booking : IValidatableObject
{
    [Key]
    public int BookingID { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Изберете потребител.")]
    [Display(Name = "Потребител")]
    public int UserID { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Изберете игрище.")]
    [Display(Name = "Игрище")]
    public int FieldID { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Дата")]
    public DateOnly BookingDate { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Начален час")]
    public TimeOnly StartTime { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Краен час")]
    public TimeOnly EndTime { get; set; }

    [Required]
    [RegularExpression("^(Активна|Отказана)$", ErrorMessage = "Невалиден статус.")]
    [Display(Name = "Статус")]
    public string Status { get; set; } = "Активна";

    [ValidateNever]
    public User? User { get; set; }

    [ValidateNever]
    public Field? Field { get; set; }

    [ValidateNever]
    public Payment? Payment { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BookingDate == default)
            yield return new ValidationResult("Изберете дата.", new[] { nameof(BookingDate) });
        else if (BookingDate < DateOnly.FromDateTime(DateTime.Today))
            yield return new ValidationResult("Не може да резервирате минала дата.", new[] { nameof(BookingDate) });

        if (EndTime <= StartTime)
            yield return new ValidationResult("Крайният час трябва да е след началния.", new[] { nameof(EndTime) });
    }
}
