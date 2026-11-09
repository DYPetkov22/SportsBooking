using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SportsBooking.Web.ViewModels;

namespace SportsBooking.Web.Reporting;

public static class ReportExports
{
    public static byte[] ToExcel(ReportsDashboardViewModel model)
    {
        using var workbook = new XLWorkbook();
        workbook.Properties.Author = "SportsBooking";
        workbook.Properties.Title = "SportsBooking - справки";

        var summary = workbook.Worksheets.Add("Обобщение");
        DecorateTitle(summary, "SportsBooking | Бизнес справка", 2);
        summary.Cell(2, 1).Value = "Период";
        summary.Cell(2, 2).Value = $"{model.From:dd.MM.yyyy} - {model.To:dd.MM.yyyy}";
        var totals = new (string Label, double Value)[]
        {
            ("Общо резервации", model.TotalBookings),
            ("Активни резервации", model.ActiveBookings),
            ("Отказани резервации", model.CancelledBookings),
            ("Брой плащания", model.PaymentsCount),
            ("Приходи (EUR)", (double)model.TotalRevenue),
            ("Общо потребители (към момента)", model.TotalUsers),
            ("Общо игрища (към момента)", model.TotalFields)
        };
        for (var i = 0; i < totals.Length; i++)
        {
            summary.Cell(i + 4, 1).Value = totals[i].Label;
            summary.Cell(i + 4, 2).Value = totals[i].Value;
        }
        summary.Cell(8, 2).Style.NumberFormat.Format = "#,##0.00";
        summary.Cell(12, 1).Value = "Приходите са по UTC дата на плащане; резервациите - по дата на игра.";
        summary.Cell(13, 1).Value = "За исторически плащания от старата база датата е тази на обновяването на схемата.";
        summary.Columns(1, 2).AdjustToContents();

        var months = workbook.Worksheets.Add("По месеци");
        DecorateTitle(months, "Месечни резервации и приходи", 5);
        SetHeaders(months, 3, "Месец", "Резервации", "Активни", "Отказани", "Приходи EUR");
        for (int i = 0; i < model.Months.Count; i++)
        {
            var row = model.Months[i]; var r = i + 4;
            months.Cell(r, 1).Value = row.Label;
            months.Cell(r, 2).Value = row.Bookings;
            months.Cell(r, 3).Value = row.ActiveBookings;
            months.Cell(r, 4).Value = row.CancelledBookings;
            months.Cell(r, 5).Value = row.Revenue;
        }
        if (model.Months.Count != 0)
            months.Range(4, 5, model.Months.Count + 3, 5).Style.NumberFormat.Format = "#,##0.00";
        months.SheetView.FreezeRows(3);
        months.Columns(1, 5).AdjustToContents();

        var sports = workbook.Worksheets.Add("Топ спортове");
        DecorateTitle(sports, "Най-търсени спортове (активни резервации)", 2);
        SetHeaders(sports, 3, "Спорт", "Резервации");
        for (int i = 0; i < model.TopSports.Count; i++)
        {
            sports.Cell(i + 4, 1).Value = model.TopSports[i].Sport;
            sports.Cell(i + 4, 2).Value = model.TopSports[i].Bookings;
        }
        sports.Columns(1, 2).AdjustToContents();

        var methods = workbook.Worksheets.Add("Начини на плащане");
        DecorateTitle(methods, "Плащания по метод (UTC дата)", 3);
        SetHeaders(methods, 3, "Метод", "Брой", "Обща сума EUR");
        for (int i = 0; i < model.PaymentMethods.Count; i++)
        {
            methods.Cell(i + 4, 1).Value = model.PaymentMethods[i].Method;
            methods.Cell(i + 4, 2).Value = model.PaymentMethods[i].Count;
            methods.Cell(i + 4, 3).Value = model.PaymentMethods[i].Amount;
        }
        if (model.PaymentMethods.Count != 0)
            methods.Range(4, 3, model.PaymentMethods.Count + 3, 3).Style.NumberFormat.Format = "#,##0.00";
        methods.Columns(1, 3).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void DecorateTitle(IXLWorksheet ws, string title, int width)
    {
        ws.Range(1, 1, 1, width).Merge();
        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, width).Style.Fill.BackgroundColor = XLColor.FromHtml("#17264B");
        ws.Range(1, 1, 1, width).Style.Font.FontColor = XLColor.White;
        ws.Range(1, 1, 1, width).Style.Font.Bold = true;
        ws.Row(1).Height = 30;
    }

    private static void SetHeaders(IXLWorksheet ws, int row, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++) ws.Cell(row, i + 1).Value = headers[i];
        var range = ws.Range(row, 1, row, headers.Length);
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#E7EDFA");
        range.Style.Font.Bold = true;
        ws.Row(row).Height = 25;
    }

    public static byte[] ToPdf(ReportsDashboardViewModel model)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontFamily("Lato").FontSize(10));
                page.Header().Column(header =>
                {
                    header.Item().Text("SPORTSBOOKING").Bold().FontSize(22).FontColor(Colors.Blue.Darken3);
                    header.Item().Text("Справка за резервации и плащания").FontSize(13);
                    header.Item().Text($"Период: {model.From:dd.MM.yyyy} – {model.To:dd.MM.yyyy}")
                        .FontColor(Colors.Grey.Darken1);
                    header.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });
                page.Content().Column(column =>
                {
                    column.Spacing(14);
                    column.Item().Text("Основни показатели").Bold().FontSize(14);
                    column.Item().Text($"Резервации: {model.TotalBookings}  •  Активни: {model.ActiveBookings}  •  Отказани: {model.CancelledBookings}");
                    column.Item().Text($"Плащания: {model.PaymentsCount}  •  Приходи: {model.TotalRevenue:N2} EUR");
                    column.Item().Text($"Потребители: {model.TotalUsers}  •  Игрища: {model.TotalFields} (общ брой към момента)");
                    column.Item().PaddingTop(8).Text("Месечни резултати").Bold().FontSize(14);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(2); });
                        table.Header(h =>
                        {
                            foreach (var heading in new[] { "Месец", "Всички", "Активни", "Отказани", "Приходи EUR" })
                                h.Cell().Background(Colors.Blue.Lighten4).BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten2).Padding(5).Text(heading).Bold().FontSize(9);
                        });
                        foreach (var row in model.Months)
                        {
                            foreach (var value in new[] { row.Label, row.Bookings.ToString(), row.ActiveBookings.ToString(),
                                           row.CancelledBookings.ToString(), row.Revenue.ToString("N2") })
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3)
                                    .Padding(4).Text(value).FontSize(9);
                        }
                    });
                    column.Item().PaddingTop(5).Text("Най-търсени спортове (активни резервации)").Bold().FontSize(14);
                    if (model.TopSports.Count == 0) column.Item().Text("Няма данни за периода.");
                    foreach (var sport in model.TopSports)
                        column.Item().Text($"{sport.Sport}: {sport.Bookings} резервации");
                    column.Item().PaddingTop(5).Text("Начини на плащане").Bold().FontSize(14);
                    if (model.PaymentMethods.Count == 0) column.Item().Text("Няма плащания за периода.");
                    foreach (var method in model.PaymentMethods)
                        column.Item().Text($"{method.Method}: {method.Count} бр. / {method.Amount:N2} EUR");
                    column.Item().PaddingTop(12).Text("Важно: приходите са по UTC дата на плащане, а резервациите — по дата на игра. Стари плащания, мигрирани от предишната схема, получават датата на миграцията.")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("SportsBooking • ");
                    text.CurrentPageNumber(); text.Span(" / "); text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

}
