using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SupportRoom.Providers.DocumentParsing;

namespace SupportRoom.Providers.Tests;

public class XlsxTextExtractorTests
{
    // Builds a real .xlsx (via the same OpenXml SDK the extractor reads with) rather than
    // checking in a binary fixture - CellReference must be set on every cell exactly like a
    // real Excel file would, since the extractor aligns data cells to headers by column letter,
    // not list position (Excel omits blank cells rather than writing them).
    private static MemoryStream BuildWorkbook(string sheetName, string[] headers, IReadOnlyList<string[]> rows)
    {
        var stream = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = sheetName });

            sheetData.Append(BuildRow(1, headers));
            for (var i = 0; i < rows.Count; i++)
            {
                sheetData.Append(BuildRow(i + 2, rows[i]));
            }

            workbookPart.Workbook.Save();
        }
        stream.Position = 0;
        return stream;
    }

    private static Row BuildRow(int rowNumber, IReadOnlyList<string> values)
    {
        var row = new Row { RowIndex = (uint)rowNumber };
        for (var col = 0; col < values.Count; col++)
        {
            row.Append(new Cell
            {
                CellReference = $"{CellRef(col)}{rowNumber}",
                DataType = CellValues.String,
                CellValue = new CellValue(values[col]),
            });
        }
        return row;
    }

    private static string CellRef(int columnIndex)
    {
        var name = "";
        var c = columnIndex;
        do
        {
            name = (char)('A' + c % 26) + name;
            c = c / 26 - 1;
        } while (c >= 0);
        return name;
    }

    [Fact]
    public void Extract_RendersEachRowAsHeaderValuePairs()
    {
        using var stream = BuildWorkbook("Prices", ["Name", "Price"],
            [["เมาส์", "199"], ["คีย์บอร์ด", "590"]]);

        var chunks = new XlsxTextExtractor().Extract(stream);

        var chunk = Assert.Single(chunks);
        Assert.Contains("Name: เมาส์", chunk.Text);
        Assert.Contains("Price: 199", chunk.Text);
        Assert.Contains("Name: คีย์บอร์ด", chunk.Text);
        Assert.Contains("Price: 590", chunk.Text);
        Assert.Equal("sheet-1-rows-2-3", chunk.ChunkId);
    }

    [Fact]
    public void Extract_GroupsRowsIntoMultipleChunksPastRowsPerChunk()
    {
        var rows = Enumerable.Range(1, 20).Select(i => new[] { $"Item{i}", i.ToString() }).ToList();
        using var stream = BuildWorkbook("Sheet1", ["Name", "Qty"], rows);

        var chunks = new XlsxTextExtractor().Extract(stream);

        // 20 data rows at 15-per-chunk => 2 chunks.
        Assert.Equal(2, chunks.Count);
        Assert.Contains("Name: Item1,", chunks[0].Text);
        Assert.Contains("Name: Item20,", chunks[1].Text);
    }

    [Fact]
    public void Extract_MatchesCellsToHeadersByColumnLetterNotPosition()
    {
        // Row 2 omits column A entirely (as real Excel does for a blank cell) - must not shift
        // "50" left into the "Name" column. Built directly (not via BuildWorkbook, which always
        // writes every column) to produce a genuinely sparse row.
        using var sparseStream = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(sparseStream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" });

            var headerRow = new Row { RowIndex = 1 };
            headerRow.Append(new Cell { CellReference = "A1", DataType = CellValues.String, CellValue = new CellValue("Name") });
            headerRow.Append(new Cell { CellReference = "B1", DataType = CellValues.String, CellValue = new CellValue("Price") });
            sheetData.Append(headerRow);

            var dataRow = new Row { RowIndex = 2 };
            dataRow.Append(new Cell { CellReference = "B2", DataType = CellValues.String, CellValue = new CellValue("50") });
            sheetData.Append(dataRow);

            workbookPart.Workbook.Save();
        }
        sparseStream.Position = 0;

        var chunks = new XlsxTextExtractor().Extract(sparseStream);

        var chunk = Assert.Single(chunks);
        Assert.Contains("Price: 50", chunk.Text);
        Assert.DoesNotContain("Name: 50", chunk.Text);
    }
}
