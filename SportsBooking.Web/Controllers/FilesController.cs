using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;
using SportsBooking.Web.Security;
using SportsBooking.Web.Services;

namespace SportsBooking.Web.Controllers;

[Authorize]
[Route("files")]
public sealed class FilesController(SportsBookingContext db, UserManager<ApplicationUser> users,
    LocalFileStorage storage, ILogger<FilesController> logger) : Controller
{
    private bool IsStaff => User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager);

    [HttpPost("profile/{profileId:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfile(int profileId, IFormFile? file, CancellationToken ct)
    {
        if (!await CanManageProfile(profileId, ct)) return Forbid();
        try
        {
            if (file == null) throw new InvalidDataException("Изберете изображение.");
            await storage.SaveProfileImageAsync(profileId, file, ct);
            TempData["Success"] = "Профилната снимка е запазена.";
        }
        catch (Exception ex) when (IsBadUpload(ex)) { SetFileError(ex); }
        return await RedirectToProfileAsync(profileId);
    }

    [HttpGet("profile/{profileId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> ProfileImage(int profileId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.UserID == profileId, ct)) return NotFound();
        var bytes = await storage.ReadProfileImageAsync(profileId, ct);
        if (bytes == null) return NotFound();
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(bytes, "image/jpeg");
    }

    // HTML forms submit POST, not DELETE. CSRF protection is mandatory.
    [HttpPost("profile/{profileId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProfile(int profileId, CancellationToken ct)
    {
        if (!await CanManageProfile(profileId, ct)) return Forbid();
        await storage.DeleteProfileImageAsync(profileId);
        TempData["Success"] = "Профилната снимка е премахната.";
        return await RedirectToProfileAsync(profileId);
    }

    [HttpPost("booking/{bookingId:int}/documents")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public async Task<IActionResult> UploadBookingDocument(int bookingId, IFormFile? file, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.BookingID == bookingId, ct);
        if (booking == null) return NotFound();
        if (!await CanManageBooking(booking.UserID)) return Forbid();
        try
        {
            if (file == null) throw new InvalidDataException("Изберете документ.");
            await storage.SaveBookingDocumentAsync(bookingId, file, ct);
            TempData["Success"] = "Документът е качен.";
        }
        catch (Exception ex) when (IsBadUpload(ex)) { SetFileError(ex); }
        return RedirectToAction("Details", "Bookings", new { id = bookingId });
    }

    [HttpGet("booking/{bookingId:int}/documents/{uploadId}")]
    public async Task<IActionResult> DownloadBookingDocument(int bookingId, string uploadId, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.BookingID == bookingId, ct);
        if (booking == null) return NotFound();
        if (!await CanManageBooking(booking.UserID)) return Forbid();
        var entry = await storage.GetBookingDocumentAsync(bookingId, uploadId, ct);
        if (entry == null) return NotFound();
        var content = await storage.ReadBookingDocumentAsync(bookingId, uploadId, ct);
        if (content == null) return NotFound();
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        // Force download (not inline HTML/PDF rendering), using the sanitized original name.
        return File(content, entry.ContentType, entry.OriginalFileName);
    }

    [HttpPost("booking/{bookingId:int}/documents/{uploadId}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBookingDocument(int bookingId, string uploadId, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.BookingID == bookingId, ct);
        if (booking == null) return NotFound();
        if (!await CanManageBooking(booking.UserID)) return Forbid();
        if (await storage.GetBookingDocumentAsync(bookingId, uploadId, ct) == null) return NotFound();
        await storage.DeleteBookingDocumentAsync(bookingId, uploadId, ct);
        TempData["Success"] = "Документът е изтрит.";
        return RedirectToAction("Details", "Bookings", new { id = bookingId });
    }

    [Authorize(Roles = AppRoles.Staff)]
    [HttpPost("field/{fieldId:int}/images")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> UploadFieldImage(int fieldId, IFormFile? file, CancellationToken ct)
    {
        if (!await db.Fields.AnyAsync(x => x.FieldID == fieldId, ct)) return NotFound();
        try
        {
            if (file == null) throw new InvalidDataException("Изберете изображение.");
            await storage.SaveFieldImageAsync(fieldId, file, ct);
            TempData["Success"] = "Снимката е добавена в галерията.";
        }
        catch (Exception ex) when (IsBadUpload(ex)) { SetFileError(ex); }
        return RedirectToAction("Details", "Fields", new { id = fieldId });
    }

    [AllowAnonymous]
    [HttpGet("field/{fieldId:int}/images/{uploadId}")]
    public async Task<IActionResult> FieldImage(int fieldId, string uploadId, CancellationToken ct)
    {
        if (!await db.Fields.AnyAsync(x => x.FieldID == fieldId, ct)) return NotFound();
        if (await storage.GetFieldImageAsync(fieldId, uploadId, ct) == null) return NotFound();
        var image = await storage.ReadFieldImageAsync(fieldId, uploadId, ct);
        if (image == null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(image, "image/jpeg");
    }

    [Authorize(Roles = AppRoles.Staff)]
    [HttpPost("field/{fieldId:int}/images/{uploadId}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFieldImage(int fieldId, string uploadId, CancellationToken ct)
    {
        if (!await db.Fields.AnyAsync(x => x.FieldID == fieldId, ct)) return NotFound();
        if (await storage.GetFieldImageAsync(fieldId, uploadId, ct) == null) return NotFound();
        await storage.DeleteFieldImageAsync(fieldId, uploadId, ct);
        TempData["Success"] = "Снимката е премахната от галерията.";
        return RedirectToAction("Details", "Fields", new { id = fieldId });
    }

    private async Task<bool> CanManageProfile(int profileId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.UserID == profileId, ct)) return false;
        if (User.IsInRole(AppRoles.Admin)) return true;
        return (await users.GetUserAsync(User))?.BookingUserId == profileId;
    }

    private async Task<bool> CanManageBooking(int bookingUserId)
    {
        if (IsStaff) return true;
        return (await users.GetUserAsync(User))?.BookingUserId == bookingUserId;
    }

    private async Task<IActionResult> RedirectToProfileAsync(int profileId)
    {
        var actor = await users.GetUserAsync(User);
        return actor?.BookingUserId == profileId
            ? RedirectToAction("Profile", "Account")
            : RedirectToAction("Edit", "Users", new { id = profileId });
    }

    private static bool IsBadUpload(Exception ex) =>
        ex is InvalidDataException or UnknownImageFormatException or InvalidImageContentException
            or NotSupportedException;

    private void SetFileError(Exception ex)
    {
        logger.LogWarning("Invalid upload: {ExceptionType}", ex.GetType().Name);
        TempData["FileError"] = ex is InvalidDataException ? ex.Message : "Невалиден или повреден файл.";
    }
}
