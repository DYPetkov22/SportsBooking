using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;
var endpoint = Environment.GetEnvironmentVariable("SPORTSBOOKING_API_URL") ?? "http://localhost:5180";
using var http = new HttpClient { BaseAddress = new Uri(endpoint), Timeout = TimeSpan.FromSeconds(15) };
int number = 1;
if (args.Length != 0 && (args.Length != 2 || args[0] is not ("page" or "field") ||
    !int.TryParse(args[1], out number) || number < 1 || (args[0] == "page" && number > 10000)))
{
    Console.Error.WriteLine("Употреба: SportsBooking.Client [page НОМЕР | field ID]");
    return 2;
}

try
{
    if (args.FirstOrDefault() == "field")
    {
        using var response = await http.GetAsync($"api/fields/{number}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.Error.WriteLine("Няма терен с този номер.");
            return 1;
        }
        response.EnsureSuccessStatusCode();
        Print(await response.Content.ReadFromJsonAsync<FieldInfo>() ?? throw new JsonException());
    }
    else
    {
        var fields = await http.GetFromJsonAsync<FieldInfo[]>($"api/fields?page={number}") ?? throw new JsonException();
        Console.WriteLine($"Терени • страница {number}");
        foreach (var field in fields) Print(field);
        if (fields.Length == 0) Console.WriteLine("На тази страница няма терени.");
    }
    return 0;
}
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
{
    Console.Error.WriteLine($"Грешка при зареждане от API: {ex.Message}");
    return 1;
}

static void Print(FieldInfo field) => Console.WriteLine(
    $"#{field.Id} {field.Name} | {field.Sport} | {field.Center}, {field.City} | {field.PricePerHour:F2} EUR/ч");

internal sealed record FieldInfo(int Id, string Name, string Center, string City, string Sport, decimal PricePerHour);
