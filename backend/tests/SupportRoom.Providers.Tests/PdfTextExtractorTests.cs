using SupportRoom.Providers.DocumentParsing;

namespace SupportRoom.Providers.Tests;

public class PdfTextExtractorTests
{
    // ---- FixThaiPuaGlyphs ---------------------------------------------------
    //
    // Same PUA fixup table as PdfSlidesRenderer's (see PdfSlidesRendererNarrationTests for the
    // full explanation of why the inputs/outputs below use explicit \u escapes rather than
    // literal glyphs).

    [Fact]
    public void FixThaiPuaGlyphs_RestoresMaiThoAndMaiEk_InARealWord()
    {
        var broken = "\u0E1C\u0E39\uF70B\u0E0A\uF70A\u0E27\u0E22";
        var expected = "\u0E1C\u0E39\u0E49\u0E0A\u0E48\u0E27\u0E22";

        Assert.Equal(expected, PdfTextExtractor.FixThaiPuaGlyphs(broken));
    }

    [Fact]
    public void FixThaiPuaGlyphs_RestoresSaraIAndSaraII_AndThanthakhat()
    {
        Assert.Equal("\u0E1B\u0E34\u0E14", PdfTextExtractor.FixThaiPuaGlyphs("\u0E1B\uF701\u0E14"));
        Assert.Equal("\u0E1F\u0E35", PdfTextExtractor.FixThaiPuaGlyphs("\u0E1F\uF702"));
        Assert.Equal("\u0E23\u0E4C", PdfTextExtractor.FixThaiPuaGlyphs("\u0E23\uF70E"));
    }

    [Fact]
    public void FixThaiPuaGlyphs_LeavesOrdinaryTextUntouched()
    {
        const string ordinary = "School Bright \u0E42\u0E23\u0E07\u0E40\u0E23\u0E35\u0E22\u0E19 123";

        Assert.Equal(ordinary, PdfTextExtractor.FixThaiPuaGlyphs(ordinary));
    }

    // ---- JoinContentOrderedLines ------------------------------------------------

    [Fact]
    public void JoinContentOrderedLines_StripsBulletMarkers()
    {
        var result = PdfTextExtractor.JoinContentOrderedLines("\u25CFFirst point\n\u25CBSecond point");

        Assert.Equal("First point. Second point.", result);
    }

    [Fact]
    public void JoinContentOrderedLines_AddsAFullStopBetweenLines_SoTheyDontRunTogether()
    {
        var result = PdfTextExtractor.JoinContentOrderedLines("First paragraph\nSecond paragraph");

        Assert.Equal("First paragraph. Second paragraph.", result);
    }

    [Fact]
    public void JoinContentOrderedLines_DoesNotDoubleUpPunctuation_WhenALineAlreadyEndsWithIt()
    {
        var result = PdfTextExtractor.JoinContentOrderedLines("What is this?\nAn example:");

        Assert.Equal("What is this? An example:", result);
    }

    [Fact]
    public void JoinContentOrderedLines_DropsBlankLines()
    {
        var result = PdfTextExtractor.JoinContentOrderedLines("First\n\n\nSecond");

        Assert.Equal("First. Second.", result);
    }

    [Fact]
    public void JoinContentOrderedLines_OfEmptyInput_ReturnsEmptyString()
    {
        Assert.Equal("", PdfTextExtractor.JoinContentOrderedLines(""));
    }
}
