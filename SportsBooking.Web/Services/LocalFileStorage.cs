using System.IO.Compression;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace SportsBooking.Web.Services;

// Sidecar metadata is stored outside wwwroot, so the existing 6-table SQL schema is unchanged.
public sealed record StoredUpload(string Id, string OriginalFileName, string ContentType,
    long Length, DateTimeOffset UploadedUtc);

public sealed class LocalFileStorage(IWebHostEnvironment environment)
{
    public const long MaxImageBytes = 5 * 1024 * 1024;
    public const long MaxDocumentBytes = 10 * 1024 * 1024;
    public const int MaxImagesPerField = 12;
    public const int MaxDocumentsPerBooking = 10;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly byte[] PngMagic = [137, 80, 78, 71, 13, 10, 26, 10];

    private string Root => Path.Combine(environment.ContentRootPath, "App_Data", "uploads");
    private string ProfilePath(int id) => Path.Combine(Root, "profiles", $"{id}.jpg");
    private string DocumentsPath(int id) => Path.Combine(Root, "bookings", id.ToString());
    private string ImagesPath(int id) => Path.Combine(Root, "fields", id.ToString());

    public async Task SaveProfileImageAsync(int profileId, IFormFile file, CancellationToken ct)
    {
        ValidatePositiveId(profileId);
        var bytes = await ReadValidatedAsync(file, MaxImageBytes, ct);
        var jpeg = await ProcessImageAsync(bytes, file, 512, ct);
        var path = ProfilePath(profileId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await WriteAtomicallyAsync(path, jpeg, ct, overwrite: true);
    }

    public async Task<byte[]?> ReadProfileImageAsync(int profileId, CancellationToken ct)
    {
        if (profileId <= 0 || !File.Exists(ProfilePath(profileId))) return null;
        return await File.ReadAllBytesAsync(ProfilePath(profileId), ct);
    }

    public Task DeleteProfileImageAsync(int profileId)
    {
        ValidatePositiveId(profileId);
        File.Delete(ProfilePath(profileId)); // No error when the file does not exist.
        return Task.CompletedTask;
    }

    public async Task<StoredUpload> SaveFieldImageAsync(int fieldId, IFormFile file, CancellationToken ct)
    {
        ValidatePositiveId(fieldId);
        var bytes = await ReadValidatedAsync(file, MaxImageBytes, ct);
        var jpeg = await ProcessImageAsync(bytes, file, 1280, ct);
        return await SaveWithMetadataAsync(ImagesPath(fieldId), file.FileName,
            "image/jpeg", jpeg, MaxImagesPerField, ct);
    }

    public async Task<StoredUpload> SaveBookingDocumentAsync(int bookingId, IFormFile file, CancellationToken ct)
    {
        ValidatePositiveId(bookingId);
        var bytes = await ReadValidatedAsync(file, MaxDocumentBytes, ct);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string contentType;
        if (extension == ".pdf" && bytes.AsSpan().StartsWith("%PDF-"u8))
            contentType = "application/pdf";
        else if (extension == ".docx" && IsWordDocument(bytes))
            contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        else
            throw new InvalidDataException("Допускат се само валидни PDF и DOCX документи.");
        return await SaveWithMetadataAsync(DocumentsPath(bookingId), file.FileName,
            contentType, bytes, MaxDocumentsPerBooking, ct);
    }

    public Task<IReadOnlyList<StoredUpload>> ListFieldImagesAsync(int id, CancellationToken ct) =>
        ListAsync(ImagesPath(id), ct);

    public Task<IReadOnlyList<StoredUpload>> ListBookingDocumentsAsync(int id, CancellationToken ct) =>
        ListAsync(DocumentsPath(id), ct);

    public Task<StoredUpload?> GetFieldImageAsync(int id, string uploadId, CancellationToken ct) =>
        GetAsync(ImagesPath(id), uploadId, ct);

    public Task<StoredUpload?> GetBookingDocumentAsync(int id, string uploadId, CancellationToken ct) =>
        GetAsync(DocumentsPath(id), uploadId, ct);

    public async Task<byte[]?> ReadFieldImageAsync(int id, string uploadId, CancellationToken ct) =>
        await ReadAsync(ImagesPath(id), uploadId, ct);

    public async Task<byte[]?> ReadBookingDocumentAsync(int id, string uploadId, CancellationToken ct) =>
        await ReadAsync(DocumentsPath(id), uploadId, ct);

    public Task DeleteFieldImageAsync(int id, string uploadId, CancellationToken ct) =>
        DeleteAsync(ImagesPath(id), uploadId, ct);

    public Task DeleteBookingDocumentAsync(int id, string uploadId, CancellationToken ct) =>
        DeleteAsync(DocumentsPath(id), uploadId, ct);

    public Task DeleteAllFieldImagesAsync(int id, CancellationToken ct) => DeleteDirectoryAsync(ImagesPath(id), ct);
    public Task DeleteAllBookingDocumentsAsync(int id, CancellationToken ct) => DeleteDirectoryAsync(DocumentsPath(id), ct);

    private static async Task<byte[]> ReadValidatedAsync(IFormFile file, long maxBytes, CancellationToken ct)
    {
        if (file == null || file.Length == 0 || file.Length > maxBytes)
            throw new InvalidDataException($"Изберете файл с размер до {maxBytes / 1024 / 1024} MB.");
        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        if (buffer.Length == 0 || buffer.Length > maxBytes)
            throw new InvalidDataException("Невалиден размер на файла.");
        return buffer.ToArray();
    }

    private static async Task<byte[]> ProcessImageAsync(byte[] bytes, IFormFile file, int maxPixels, CancellationToken ct)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        bool isJpeg = bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
        bool isPng = bytes.AsSpan().StartsWith(PngMagic);
        if (!((extension is ".jpg" or ".jpeg") && isJpeg || extension == ".png" && isPng))
            throw new InvalidDataException("Допускат се само реални JPEG и PNG изображения.");
        if ((isJpeg && !string.Equals(file.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase)) ||
            (isPng && !string.Equals(file.ContentType, "image/png", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Невалиден MIME тип на изображението.");
        // Check dimensions BEFORE decoding to avoid excessive memory use.
        await using var input = new MemoryStream(bytes, writable: false);
        var dimensions = await Image.IdentifyAsync(input, ct);
        if (dimensions == null || dimensions.Width <= 0 || dimensions.Height <= 0 ||
            dimensions.Width > 8000 || dimensions.Height > 8000 ||
            (long)dimensions.Width * dimensions.Height > 32_000_000)
            throw new InvalidDataException("Размерите на изображението са прекалено големи.");
        input.Position = 0;
        using var image = await Image.LoadAsync(input, ct);
        image.Mutate(x => x.AutoOrient().Resize(new ResizeOptions
        {
            Size = new Size(maxPixels, maxPixels), Mode = ResizeMode.Max
        }));
        // Do not retain EXIF/GPS and other potentially private camera metadata.
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.CicpProfile = null;
        await using var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 82 }, ct);
        return output.ToArray();
    }

    private static bool IsWordDocument(byte[] bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0x50 || bytes[1] != 0x4B) return false;
        try
        {
            using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
            return archive.GetEntry("[Content_Types].xml") != null &&
                   archive.GetEntry("word/document.xml") != null &&
                   archive.GetEntry("word/vbaProject.bin") == null;
        }
        catch (InvalidDataException) { return false; }
    }

    private async Task<StoredUpload> SaveWithMetadataAsync(string directory, string fileName,
        string contentType, byte[] content, int maximumCount, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(directory);
            if (Directory.EnumerateFiles(directory, "*.json").Count() >= maximumCount)
                throw new InvalidDataException($"Максималният брой файлове е {maximumCount}.");
            var id = Guid.NewGuid().ToString("N");
            var name = Path.GetFileName(fileName.Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(name)) name = "file";
            name = new string(name.Where(c => !char.IsControl(c) && c != '"' && c != '\\' && c != '/').Take(120).ToArray());
            var metadata = new StoredUpload(id, name, contentType, content.Length, DateTimeOffset.UtcNow);
            var contentPath = ContentPath(directory, id);
            await WriteAtomicallyAsync(contentPath, content, ct);
            try
            {
                var json = JsonSerializer.SerializeToUtf8Bytes(metadata, JsonOptions);
                await WriteAtomicallyAsync(MetadataPath(directory, id), json, ct);
            }
            catch { File.Delete(contentPath); throw; }
            return metadata;
        }
        finally { _gate.Release(); }
    }

    private static async Task<IReadOnlyList<StoredUpload>> ListAsync(string directory, CancellationToken ct)
    {
        if (!Directory.Exists(directory)) return [];
        var results = new List<StoredUpload>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var entry = JsonSerializer.Deserialize<StoredUpload>(await File.ReadAllBytesAsync(path, ct), JsonOptions);
                if (entry != null && Guid.TryParseExact(entry.Id, "N", out _) &&
                    File.Exists(ContentPath(directory, entry.Id))) results.Add(entry);
            }
            catch (JsonException) { /* Ignore damaged metadata rather than crash the whole list. */ }
            catch (IOException) { /* An upload may be being removed. */ }
        }
        return results.OrderByDescending(x => x.UploadedUtc).ToList();
    }

    private static async Task<StoredUpload?> GetAsync(string directory, string uploadId, CancellationToken ct)
    {
        if (!IsValidUploadId(uploadId)) return null;
        var path = MetadataPath(directory, uploadId);
        if (!File.Exists(path) || !File.Exists(ContentPath(directory, uploadId))) return null;
        try
        {
            var entry = JsonSerializer.Deserialize<StoredUpload>(await File.ReadAllBytesAsync(path, ct), JsonOptions);
            return entry?.Id == uploadId ? entry : null;
        }
        catch (JsonException) { return null; }
        catch (FileNotFoundException) { return null; }
    }

    private static async Task<byte[]?> ReadAsync(string directory, string uploadId, CancellationToken ct)
    {
        if (!IsValidUploadId(uploadId)) return null;
        var path = ContentPath(directory, uploadId);
        if (!File.Exists(path)) return null;
        try { return await File.ReadAllBytesAsync(path, ct); }
        catch (FileNotFoundException) { return null; }
    }

    private async Task DeleteAsync(string directory, string uploadId, CancellationToken ct)
    {
        if (!IsValidUploadId(uploadId)) return;
        await _gate.WaitAsync(ct);
        try
        {
            File.Delete(ContentPath(directory, uploadId));
            File.Delete(MetadataPath(directory, uploadId));
        }
        finally { _gate.Release(); }
    }

    private async Task DeleteDirectoryAsync(string directory, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
        finally { _gate.Release(); }
    }

    private static async Task WriteAtomicallyAsync(string path, byte[] bytes, CancellationToken ct, bool overwrite = false)
    {
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temp, bytes, ct);
            File.Move(temp, path, overwrite);
        }
        finally { File.Delete(temp); }
    }

    private static bool IsValidUploadId(string id) => Guid.TryParseExact(id, "N", out _);
    private static string ContentPath(string directory, string id) => Path.Combine(directory, id + ".bin");
    private static string MetadataPath(string directory, string id) => Path.Combine(directory, id + ".json");
    private static void ValidatePositiveId(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
    }
}
