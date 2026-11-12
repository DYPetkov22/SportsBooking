using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SportsBooking.Web.Controllers;

[AllowAnonymous, IgnoreAntiforgeryToken]
public sealed class ErrorsController : Controller
{
    [Route("Errors/{statusCode:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusPage(int statusCode)
    {
        if (statusCode < 400 || statusCode > 599) statusCode = 500;
        Response.StatusCode = statusCode;
        return View(statusCode == 404 ? "NotFound" : "ServerError", statusCode);
    }
}
