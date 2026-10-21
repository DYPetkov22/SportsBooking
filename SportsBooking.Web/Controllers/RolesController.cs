using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Models;
using SportsBooking.Web.Security;
using SportsBooking.Web.ViewModels;

namespace SportsBooking.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class RolesController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    public RolesController(UserManager<ApplicationUser> users) => _users = users;

    public async Task<IActionResult> Index()
    {
        var accounts = await _users.Users.AsNoTracking().OrderBy(x => x.Email).ToListAsync();
        var rows = new List<RoleUserRow>();
        foreach (var account in accounts)
        {
            var roles = await _users.GetRolesAsync(account);
            rows.Add(new RoleUserRow {
                Id = account.Id,
                Email = account.Email ?? "",
                CurrentRole = roles.FirstOrDefault() ?? "(няма)",
                IsCurrentUser = account.Id == _users.GetUserId(User)
            });
        }
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string id, string role)
    {
        if (role != AppRoles.Admin && role != AppRoles.Manager && role != AppRoles.User)
            return BadRequest("Невалидна роля.");
        var account = await _users.FindByIdAsync(id);
        if (account == null) return NotFound();
        if (account.Id == _users.GetUserId(User))
            return BadRequest("Не можете да промените собствената си роля.");

        var oldRoles = await _users.GetRolesAsync(account);
        // Един акаунт има точно една от трите роли.
        var removed = await _users.RemoveFromRolesAsync(account, oldRoles);
        if (!removed.Succeeded)
        {
            TempData["Error"] = "Старата роля не можа да бъде премахната.";
            return RedirectToAction(nameof(Index));
        }
        var added = await _users.AddToRoleAsync(account, role);
        if (!added.Succeeded)
        {
            if (oldRoles.Count > 0) await _users.AddToRolesAsync(account, oldRoles);
            TempData["Error"] = "Новата роля не можа да бъде запазена.";
            return RedirectToAction(nameof(Index));
        }
        await _users.UpdateSecurityStampAsync(account); // При следваща проверка се обновява сесията.
        TempData["Success"] = "Ролята е променена. Потребителят може да влезе отново.";
        return RedirectToAction(nameof(Index));
    }
}
