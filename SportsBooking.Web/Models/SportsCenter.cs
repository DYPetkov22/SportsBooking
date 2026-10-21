using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SportsBooking.Web.Models;

public class SportsCenter
{
    [Key]
    public int CenterID { get; set; }

    [Required(ErrorMessage = "Въведете име на спортния център.")]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Спортен център")]
    public string CenterName { get; set; } = "";

    [Required(ErrorMessage = "Въведете град.")]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Град")]
    public string City { get; set; } = "";

    [ValidateNever]
    public ICollection<Field> Fields { get; set; } = new List<Field>();
}
