using ClosedXML.Excel;

namespace CMS.Application.Helpers;

public record XCol(string Header, double Width, string? Format = null, bool Center = false);

public static class TableExcel
{
    public const string Qty = "#,##0.###;[Red]-#,##0.###";
    public const string QtyDash = "#,##0.###;[Red]-#,##0.###;\"-\"";       // zero shows as a dash
    public const string Money = "#,##0.00";
    public const string Day = "yyyy-MM-dd";

    private static readonly XLColor Navy = XLColor.FromHtml("#1F3864");
    private static readonly XLColor Blue = XLColor.FromHtml("#2F5597");
    private static readonly XLColor Light = XLColor.FromHtml("#DDEBF7");
    private static readonly XLColor Zebra = XLColor.FromHtml("#F4F7FC");
    private static readonly XLColor Line = XLColor.FromHtml("#BFC9D9");

    /// <param name="totals">same length as the columns; null cells stay empty, the first cell may hold the label</param>
    /// 
    public static byte[] Build(string sheet, string title, string subtitle, IReadOnlyList<XCol> cols,
                           IEnumerable<object?[]> rows, object?[]? totals = null, string totalsLabel = "الاجمالي")
    {
        using var wb = new XLWorkbook();
        AddSheet(wb, sheet, title, subtitle, cols, rows, totals, totalsLabel);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
    public static void AddSheet(XLWorkbook wb, string sheet, string title, string subtitle, IReadOnlyList<XCol> cols,
                            IEnumerable<object?[]> rows, object?[]? totals = null, string totalsLabel = "الاجمالي")
    {
        var ws = wb.AddWorksheet(sheet);
        ws.RightToLeft = true;
        ws.ShowGridLines = false;
        ws.Style.Font.FontName = "Tahoma";
        ws.Style.Font.FontSize = 11;
        ws.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        var n = cols.Count;
        for (var c = 0; c < n; c++) ws.Column(c + 1).Width = cols[c].Width;

        var r = 1;
        var banner = ws.Range(r, 1, r, n).Merge();
        banner.Value = title;
        banner.Style.Font.Bold = true;
        banner.Style.Font.FontSize = 16;
        banner.Style.Font.FontColor = XLColor.White;
        banner.Style.Fill.BackgroundColor = Navy;
        banner.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(r).Height = 32;
        r++;

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            var sub = ws.Range(r, 1, r, n).Merge();
            sub.Value = subtitle;
            sub.Style.Font.Italic = true;
            sub.Style.Font.FontColor = XLColor.FromHtml("#555555");
            sub.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sub.Style.Alignment.WrapText = true;
            ws.Row(r).Height = 22;
            r++;
        }
        r++;

        var headerRow = r;
        for (var c = 0; c < n; c++) ws.Cell(r, c + 1).Value = cols[c].Header;
        var head = ws.Range(r, 1, r, n);
        head.Style.Font.Bold = true;
        head.Style.Font.FontColor = XLColor.White;
        head.Style.Fill.BackgroundColor = Blue;
        head.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        head.Style.Alignment.WrapText = true;
        ws.Row(r).Height = 26;
        r++;

        var idx = 0;
        foreach (var row in rows)
        {
            for (var c = 0; c < n; c++)
            {
                var cell = ws.Cell(r, c + 1);
                Put(cell, c < row.Length ? row[c] : null);
                if (cols[c].Format != null) cell.Style.NumberFormat.Format = cols[c].Format;
                if (cols[c].Center) cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            if (idx++ % 2 == 1) ws.Range(r, 1, r, n).Style.Fill.BackgroundColor = Zebra;
            r++;
        }
        var lastData = r - 1;

        if (totals != null)
        {
            for (var c = 0; c < n; c++)
            {
                var cell = ws.Cell(r, c + 1);
                Put(cell, c < totals.Length ? totals[c] : null);
                if (cols[c].Format != null) cell.Style.NumberFormat.Format = cols[c].Format;
                if (cols[c].Center) cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            if (totals.Length == 0 || totals[0] == null) ws.Cell(r, 1).Value = totalsLabel;

            var t = ws.Range(r, 1, r, n);
            t.Style.Font.Bold = true;
            t.Style.Fill.BackgroundColor = Light;
            t.Style.Border.TopBorder = XLBorderStyleValues.Medium;
            t.Style.Border.TopBorderColor = Navy;
            r++;
        }

        var all = ws.Range(headerRow, 1, r - 1, n);
        all.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        all.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        all.Style.Border.OutsideBorderColor = Line;
        all.Style.Border.InsideBorderColor = Line;

        ws.SheetView.FreezeRows(headerRow);
        if (lastData >= headerRow + 1) ws.Range(headerRow, 1, lastData, n).SetAutoFilter();

        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.Margins.Left = ws.PageSetup.Margins.Right = 0.4;
        ws.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);
        ws.PageSetup.Footer.Center.AddText(XLHFPredefinedText.PageNumber, XLHFOccurrence.AllPages);

        //using var ms = new MemoryStream();
        //wb.SaveAs(ms);
        //return ms.ToArray();
    }

    //public static byte[] Build(string sheet, string title, string subtitle, IReadOnlyList<XCol> cols,
    //                           IEnumerable<object?[]> rows, object?[]? totals = null, string totalsLabel = "الاجمالي")
    //{
    //    using var wb = new XLWorkbook();
        
    //}

    private static void Put(IXLCell c, object? v)
    {
        switch (v)
        {
            case null: break;
            case string s: c.Value = s; break;
            case decimal d: c.Value = d; break;
            case long l: c.Value = l; break;
            case int i: c.Value = i; break;
            case DateTime dt: c.Value = dt.Date; break;
            default: c.Value = v.ToString(); break;
        }
    }
}