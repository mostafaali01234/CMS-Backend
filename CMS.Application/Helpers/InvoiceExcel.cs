// CMS.Application/Services/InvoiceExcel.cs
using ClosedXML.Excel;
using System.Drawing;

namespace CMS.Application.Helpers;

public record InvoiceSheetItem(string Product, string Store, string Project, string PriceType,
                               decimal Price, decimal Qty, decimal Total, decimal Discount, decimal Net, string Notes);
public record InvoiceSheetPayment(DateTime Date, string Kind, string Safe, decimal Amount, decimal AmountCurrency,
                                  string Notes, string By);
public record InvoiceSheetSummary(string Label, decimal Egp, decimal Cur);

public class InvoiceSheet
{
    public string Title { get; set; } = "";
    public string PartyLabel { get; set; } = "";
    public string PartyName { get; set; } = "";
    public long Number { get; set; }
    public DateTime Date { get; set; }
    public string Notes { get; set; } = "";
    public string Auditor { get; set; } = "";
    public long? OriginalNumber { get; set; }
    public long? OrderId { get; set; }
    public List<InvoiceSheetItem> Items { get; set; } = new();
    public List<InvoiceSheetPayment> Payments { get; set; } = new();
    public List<InvoiceSheetSummary> Summary { get; set; } = new();
}

public static class InvoiceExcel
{
    // palette
    private static readonly XLColor Navy = XLColor.FromHtml("#1F3864");
    private static readonly XLColor Blue = XLColor.FromHtml("#2F5597");
    private static readonly XLColor Light = XLColor.FromHtml("#DDEBF7");
    private static readonly XLColor Zebra = XLColor.FromHtml("#F4F7FC");
    private static readonly XLColor Gold = XLColor.FromHtml("#FFF2CC");
    private static readonly XLColor Line = XLColor.FromHtml("#BFC9D9");
    private static readonly XLColor Red = XLColor.FromHtml("#C00000");

    private const string Money = "#,##0.00";
    private const string MoneyDash = "#,##0.00;-#,##0.00;\"-\"";   // zero shows as a dash
    private const string Qty = "#,##0.###";
    private const int Cols = 10;

    public static string PriceTypeLabel(object? v) => v?.ToString() == "Currency" ? "عملة" : "جنيه";

    public static byte[] Build(InvoiceSheet s)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("الفاتورة");
        ws.RightToLeft = true;
        ws.ShowGridLines = false;
        ws.Style.Font.FontName = "Tahoma";
        ws.Style.Font.FontSize = 11;
        ws.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        var widths = new double[] { 30, 18, 16, 12, 13, 10, 14, 13, 14, 26 };
        for (var c = 0; c < widths.Length; c++) ws.Column(c + 1).Width = widths[c];

        var r = 1;

        // ---- title banner ----
        var title = ws.Range(r, 1, r, Cols).Merge();
        title.Value = $"{s.Title}  #{s.Number}";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 18;
        title.Style.Font.FontColor = XLColor.White;
        title.Style.Fill.BackgroundColor = Navy;
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(r).Height = 36;
        r += 2;

        // ---- details block: two label/value pairs per row ----
        var pairs = new List<(string Key, string Value)>
        {
            ("رقم الفاتورة", s.Number.ToString()),
            ("التاريخ", s.Date.ToString("yyyy-MM-dd")),
            (s.PartyLabel, s.PartyName),
        };
        if (s.OrderId.HasValue) pairs.Add(("رقم الاوردر", s.OrderId.Value.ToString()));
        if (s.OriginalNumber.HasValue) pairs.Add(("الفاتورة الاصلية", s.OriginalNumber.Value.ToString()));
        if (!string.IsNullOrWhiteSpace(s.Auditor)) pairs.Add(("المسجل", s.Auditor));

        var infoStart = r;
        for (var i = 0; i < pairs.Count; i += 2)
        {
            WritePair(ws, r, 1, 2, 4, pairs[i]);
            if (i + 1 < pairs.Count) WritePair(ws, r, 6, 7, 10, pairs[i + 1]);
            r++;
        }
        if (!string.IsNullOrWhiteSpace(s.Notes))
        {
            ws.Cell(r, 1).Value = "ملاحظات";
            LabelStyle(ws.Cell(r, 1));
            var n = ws.Range(r, 2, r, Cols).Merge();
            n.Value = s.Notes;
            n.Style.Alignment.WrapText = true;
            ws.Row(r).Height = Math.Max(20, Math.Ceiling(s.Notes.Length / 90.0) * 18);
            r++;
        }
        Box(ws.Range(infoStart, 1, r - 1, Cols));
        r++;

        // ---- items ----
        SectionBar(ws, r, "الأصناف");
        r++;

        var itemHeaders = new[] { "المنتج", "المخزن", "المشروع", "نوع السعر", "السعر", "الكمية", "الاجمالي", "الخصم", "الصافي", "ملاحظات" };
        HeaderRow(ws, r, itemHeaders);
        var itemsHeaderRow = r;
        r++;

        var firstItem = r;
        for (var idx = 0; idx < s.Items.Count; idx++)
        {
            var i = s.Items[idx];
            ws.Cell(r, 1).Value = i.Product;
            ws.Cell(r, 2).Value = i.Store;
            ws.Cell(r, 3).Value = i.Project;
            ws.Cell(r, 4).Value = i.PriceType;
            ws.Cell(r, 5).Value = i.Price;
            ws.Cell(r, 6).Value = i.Qty;
            ws.Cell(r, 7).Value = i.Total;
            ws.Cell(r, 8).Value = i.Discount;
            ws.Cell(r, 9).Value = i.Net;
            ws.Cell(r, 10).Value = i.Notes;

            ws.Range(r, 5, r, 5).Style.NumberFormat.Format = Money;
            ws.Cell(r, 6).Style.NumberFormat.Format = Qty;
            ws.Range(r, 7, r, 9).Style.NumberFormat.Format = Money;
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 9).Style.Font.Bold = true;
            ws.Cell(r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(r, 10).Style.Alignment.WrapText = true;

            if (idx % 2 == 1) ws.Range(r, 1, r, Cols).Style.Fill.BackgroundColor = Zebra;
            if (i.PriceType == "عملة")
            {
                // currency lines are priced in the foreign currency, so they stand out
                ws.Cell(r, 4).Style.Fill.BackgroundColor = Gold;
                ws.Range(r, 5, r, 9).Style.Font.FontColor = XLColor.FromHtml("#7F6000");
            }
            r++;
        }
        if (s.Items.Count == 0)
        {
            ws.Range(r, 1, r, Cols).Merge().Value = "لا توجد اصناف";
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.Gray;
            r++;
        }
        Grid(ws.Range(itemsHeaderRow, 1, r - 1, Cols));
        r++;

        // ---- payments ----
        if (s.Payments.Count > 0)
        {
            SectionBar(ws, r, "الدفعات");
            r++;

            var payHeaders = new[] { "التاريخ", "النوع", "الخزينة", "المبلغ", "بالعملة", "ملاحظات", "المسجل" };
            HeaderRow(ws, r, payHeaders, lastCol: payHeaders.Length);
            var payHeaderRow = r;
            r++;

            for (var idx = 0; idx < s.Payments.Count; idx++)
            {
                var p = s.Payments[idx];
                ws.Cell(r, 1).Value = p.Date.ToString("yyyy-MM-dd");
                ws.Cell(r, 2).Value = p.Kind;
                ws.Cell(r, 3).Value = p.Safe;
                ws.Cell(r, 4).Value = p.Amount;
                ws.Cell(r, 5).Value = p.AmountCurrency;
                ws.Cell(r, 6).Value = p.Notes;
                ws.Cell(r, 7).Value = p.By;
                ws.Cell(r, 4).Style.NumberFormat.Format = Money;
                ws.Cell(r, 5).Style.NumberFormat.Format = MoneyDash;
                ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (idx % 2 == 1) ws.Range(r, 1, r, payHeaders.Length).Style.Fill.BackgroundColor = Zebra;
                if (p.Kind == "استرداد") ws.Range(r, 2, r, 5).Style.Font.FontColor = Red;   // money paid out
                r++;
            }
            Grid(ws.Range(payHeaderRow, 1, r - 1, payHeaders.Length));
            r++;
        }

        // ---- summary: sits under the item totals (label | pounds | currency) ----
        var sumHeader = ws.Range(r, 7, r, 10);
        ws.Range(r, 7, r, 8).Merge().Value = "الملخص";
        ws.Cell(r, 9).Value = "جنيه";
        ws.Cell(r, 10).Value = "عملة";
        sumHeader.Style.Font.Bold = true;
        sumHeader.Style.Font.FontColor = XLColor.White;
        sumHeader.Style.Fill.BackgroundColor = Blue;
        sumHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        var sumStart = r;
        r++;

        foreach (var x in s.Summary)
        {
            ws.Range(r, 7, r, 8).Merge().Value = x.Label;
            ws.Cell(r, 9).Value = x.Egp;
            ws.Cell(r, 10).Value = x.Cur;
            ws.Cell(r, 9).Style.NumberFormat.Format = Money;
            ws.Cell(r, 10).Style.NumberFormat.Format = MoneyDash;
            ws.Cell(r, 7).Style.Font.Bold = true;

            if (x.Label is "الصافي" or "المسترد")
            {
                ws.Range(r, 7, r, 10).Style.Fill.BackgroundColor = Light;
                ws.Range(r, 7, r, 10).Style.Font.Bold = true;
            }
            if (x.Label == "المتبقي")
            {
                // what is still unpaid: red when something is owed, green when settled
                var open = x.Egp > 0 || x.Cur > 0;
                ws.Range(r, 7, r, 10).Style.Font.Bold = true;
                ws.Range(r, 7, r, 10).Style.Font.FontColor = open ? Red : XLColor.FromHtml("#375623");
                ws.Range(r, 7, r, 10).Style.Fill.BackgroundColor = open
                    ? XLColor.FromHtml("#FDE9E7") : XLColor.FromHtml("#E2EFDA");
            }
            r++;
        }
        Grid(ws.Range(sumStart, 7, r - 1, 10));

        // ---- printing ----
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);                    // one page wide, as many tall as needed
        ws.PageSetup.Margins.Left = ws.PageSetup.Margins.Right = 0.4;
        ws.PageSetup.CenterHorizontally = true;
        ws.PageSetup.SetRowsToRepeatAtTop(itemsHeaderRow, itemsHeaderRow);
        ws.PageSetup.Footer.Center.AddText(XLHFPredefinedText.PageNumber, XLHFOccurrence.AllPages);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ---------- helpers ----------

    private static void LabelStyle(IXLCell c)
    {
        c.Style.Font.Bold = true;
        c.Style.Fill.BackgroundColor = Light;
        c.Style.Font.FontColor = Navy;
    }

    private static void WritePair(IXLWorksheet ws, int row, int labelFrom, int valueFrom, int valueTo,
                                  (string Key, string Value) p)
    {
        var label = ws.Range(row, labelFrom, row, valueFrom - 1).Merge();
        label.Value = p.Key;
        label.Style.Font.Bold = true;
        label.Style.Fill.BackgroundColor = Light;
        label.Style.Font.FontColor = Navy;

        var value = ws.Range(row, valueFrom, row, valueTo).Merge();
        value.Value = p.Value;
        value.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Row(row).Height = 22;
    }

    private static void SectionBar(IXLWorksheet ws, int row, string text)
    {
        var bar = ws.Range(row, 1, row, Cols).Merge();
        bar.Value = text;
        bar.Style.Font.Bold = true;
        bar.Style.Font.FontSize = 12;
        bar.Style.Font.FontColor = Navy;
        bar.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        bar.Style.Border.BottomBorderColor = Navy;
        ws.Row(row).Height = 24;
    }

    private static void HeaderRow(IXLWorksheet ws, int row, string[] headers, int? lastCol = null)
    {
        for (var c = 0; c < headers.Length; c++) ws.Cell(row, c + 1).Value = headers[c];
        var range = ws.Range(row, 1, row, lastCol ?? headers.Length);
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = Blue;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(row).Height = 24;
    }

    private static void Grid(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = Line;
        range.Style.Border.InsideBorderColor = Line;
    }

    private static void Box(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = Line;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        range.Style.Border.InsideBorderColor = Line;
    }
}