using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace SupportRoom.Providers.DocumentParsing;

/// <summary>
/// One chunk per group of rows per worksheet - row 1 is treated as column headers so each chunk
/// reads as labeled data ("Header: Value") instead of a bare grid, which is what lets the RAG
/// answer prompt explain a spreadsheet row naturally instead of just echoing numbers. Cells are
/// matched to headers by their actual column letter (CellReference), not list position - Excel
/// omits blank cells rather than writing them empty, so positional matching would misalign
/// columns on any row with a gap.
/// </summary>
public sealed partial class XlsxTextExtractor : IDocumentTextExtractor
{
    private const int RowsPerChunk = 15;

    [GeneratedRegex(@"^[A-Z]+")]
    private static partial Regex ColumnLettersPattern();

    public IReadOnlyList<DocumentTextChunk> Extract(Stream content)
    {
        using var doc = SpreadsheetDocument.Open(content, false);
        var workbookPart = doc.WorkbookPart;
        if (workbookPart is null)
        {
            return [];
        }

        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
        var chunks = new List<DocumentTextChunk>();
        var sheetIndex = 0;

        foreach (var sheet in workbookPart.Workbook.Descendants<Sheet>())
        {
            sheetIndex++;
            var relId = sheet.Id?.Value;
            if (string.IsNullOrEmpty(relId))
            {
                continue;
            }

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(relId);
            var rows = worksheetPart.Worksheet.Descendants<Row>().ToList();
            if (rows.Count == 0)
            {
                continue;
            }

            var headers = ReadRowByColumn(rows[0], sharedStrings);
            var dataRows = rows.Skip(1).ToList();

            for (var i = 0; i < dataRows.Count; i += RowsPerChunk)
            {
                var group = dataRows.Skip(i).Take(RowsPerChunk).ToList();
                var lines = group
                    .Select(row => RenderRow(row, headers, sharedStrings))
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();
                if (lines.Count == 0)
                {
                    continue;
                }

                chunks.Add(new DocumentTextChunk
                {
                    ChunkId = $"sheet-{sheetIndex}-rows-{i + 2}-{i + group.Count + 1}",
                    Text = string.Join("\n", lines).Trim(),
                });
            }
        }

        return chunks;
    }

    private static Dictionary<int, string> ReadRowByColumn(Row row, SharedStringTable? sharedStrings)
    {
        var result = new Dictionary<int, string>();
        foreach (var cell in row.Elements<Cell>())
        {
            var value = GetCellValue(cell, sharedStrings);
            if (!string.IsNullOrWhiteSpace(value))
            {
                result[GetColumnIndex(cell)] = value;
            }
        }
        return result;
    }

    private static string RenderRow(Row row, Dictionary<int, string> headers, SharedStringTable? sharedStrings)
    {
        var parts = new List<string>();
        foreach (var cell in row.Elements<Cell>())
        {
            var value = GetCellValue(cell, sharedStrings);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var columnIndex = GetColumnIndex(cell);
            var header = headers.TryGetValue(columnIndex, out var h) && !string.IsNullOrWhiteSpace(h) ? h : $"Column{columnIndex + 1}";
            parts.Add($"{header}: {value}");
        }
        return string.Join(", ", parts);
    }

    private static int GetColumnIndex(Cell cell)
    {
        var reference = cell.CellReference?.Value ?? "";
        var letters = ColumnLettersPattern().Match(reference).Value;
        var index = 0;
        foreach (var ch in letters)
        {
            index = index * 26 + (ch - 'A' + 1);
        }
        return index - 1;
    }

    private static string GetCellValue(Cell cell, SharedStringTable? sharedStrings)
    {
        var raw = cell.CellValue?.Text ?? cell.InnerText;
        if (cell.DataType?.Value == CellValues.SharedString && sharedStrings is not null && int.TryParse(raw, out var index))
        {
            return sharedStrings.ElementAtOrDefault(index)?.InnerText ?? raw;
        }
        return raw;
    }
}
