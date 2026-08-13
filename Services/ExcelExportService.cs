using ClosedXML.Excel;

namespace optical_care_management_system.Services;

/// <summary>
/// Builds .xlsx workbooks for the admin record exports.
/// </summary>
/// <summary>One worksheet within an exported workbook.</summary>
/// <param name="Name">Worksheet tab name.</param>
/// <param name="Title">Report title placed in the first row.</param>
/// <param name="Headers">Column headings.</param>
/// <param name="Rows">Row values; each array must line up with <paramref name="Headers"/>.</param>
public record ExcelSheet(string Name, string Title, string[] Headers, IEnumerable<object?[]> Rows);

public static class ExcelExportService
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Builds a single-sheet workbook and returns its bytes.</summary>
    public static byte[] Build(string sheetName, string title, string[] headers, IEnumerable<object?[]> rows) =>
        Build(new[] { new ExcelSheet(sheetName, title, headers, rows) });

    /// <summary>Builds a workbook containing one worksheet per supplied <see cref="ExcelSheet"/>.</summary>
    public static byte[] Build(IEnumerable<ExcelSheet> sheets)
    {
        using var workbook = new XLWorkbook();

        foreach (var sheet in sheets)
        {
            AddSheet(workbook, sheet);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AddSheet(XLWorkbook workbook, ExcelSheet sheet)
    {
        var (_, title, headers, rows) = sheet;

        // Worksheet names cannot exceed 31 chars or contain : \ / ? * [ ]
        var worksheet = workbook.Worksheets.Add(SafeSheetName(sheet.Name));

        // Title banner across the full width of the table.
        worksheet.Cell(1, 1).Value = title;
        worksheet.Range(1, 1, 1, headers.Length).Merge();
        worksheet.Cell(1, 1).Style
            .Font.SetBold(true)
            .Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        worksheet.Cell(2, 1).Value = $"Generated {DateTime.Now:dd MMM yyyy HH:mm}";
        worksheet.Range(2, 1, 2, headers.Length).Merge();
        worksheet.Cell(2, 1).Style
            .Font.SetItalic(true)
            .Font.SetFontSize(9)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        const int headerRow = 4;

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold(true);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0F2D5C"));
            cell.Style.Font.SetFontColor(XLColor.White);
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        var currentRow = headerRow;

        foreach (var row in rows)
        {
            currentRow++;

            for (var i = 0; i < headers.Length; i++)
            {
                var value = i < row.Length ? row[i] : null;
                worksheet.Cell(currentRow, i + 1).SetValue(ToCellValue(value));
            }
        }

        // Border the whole block, freeze the header, and add an autofilter so
        // the sheet is usable the moment it opens in Excel.
        var dataRange = worksheet.Range(headerRow, 1, Math.Max(currentRow, headerRow), headers.Length);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.SetAutoFilter();

        worksheet.SheetView.FreezeRows(headerRow);
        worksheet.Columns().AdjustToContents();

        // Keep columns readable without letting a long note blow the width out.
        foreach (var column in worksheet.ColumnsUsed())
        {
            if (column.Width > 45)
            {
                column.Width = 45;
            }
        }
    }

    /// <summary>Builds a timestamped download name, e.g. "Patients_20260807_1432.xlsx".</summary>
    public static string FileName(string baseName) =>
        $"{baseName}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

    private static XLCellValue ToCellValue(object? value) => value switch
    {
        null => Blank.Value,
        string s => s,
        bool b => b,
        DateTime d => d,
        int i => i,
        long l => l,
        decimal m => m,
        double d => d,
        _ => value.ToString() ?? string.Empty
    };

    private static string SafeSheetName(string name)
    {
        var cleaned = new string(name.Where(c => !"[]:*?/\\".Contains(c)).ToArray());

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = "Sheet1";
        }

        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
