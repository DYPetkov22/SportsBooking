using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SportsBooking.Web.Models;

public class Sport
{
    [Key]
    public int SportID { get; set; }

    [Required(ErrorMessage = "Въведете вид спорт.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Името трябва да е от 2 до 50 символа.")]
    [Display(Name = "Спорт")]
    public string SportName { get; set; } = "";

    [ValidateNever]
    public ICollection<Field> Fields { get; set; } = new List<Field>();
}
