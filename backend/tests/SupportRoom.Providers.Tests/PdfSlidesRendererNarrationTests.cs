using SupportRoom.Providers.Slides;

namespace SupportRoom.Providers.Tests;

public class PdfSlidesRendererNarrationTests
{
    // ---- FixThaiPuaGlyphs ---------------------------------------------------
    //
    // Google Slides' PDF export embeds a subsetted Sarabun font whose cmap maps tone marks and
    // vowel signs to Private Use Area codepoints instead of their real Thai Unicode values -
    // reverse-engineered from a real exported deck (see PdfSlidesRenderer's doc comment on
    // ThaiPuaGlyphFixups for how each mapping was derived). Inputs/outputs below are built from
    // explicit \u escapes rather than literal glyphs - the PUA codepoints render as nothing in
    // most fonts/editors, which makes literal-glyph text silently easy to corrupt on copy/paste.

    [Fact]
    public void FixThaiPuaGlyphs_RestoresMaiThoAndMaiEk_InARealWord()
    {
        // Phor(0E1C) Sara-U(0E39) [PUA F70B = mai tho] Chor(0E0A) [PUA F70A = mai ek]
        // Wor(0E27) Yor(0E22) - what PdfPig hands back for the word meaning "assistant", both
        // tone marks silently swapped for their PUA glyph variant.
        var broken = "ผูชวย";
        var expected = "ผู้ช่วย";

        Assert.Equal(expected, PdfSlidesRenderer.FixThaiPuaGlyphs(broken));
    }

    [Fact]
    public void FixThaiPuaGlyphs_RestoresSaraIAndSaraII_AndThanthakhat()
    {
        // ป(0E1B) [PUA F701 = sara i] ด(0E14) -> เปิด-style word carrying sara i.
        Assert.Equal("ปิด", PdfSlidesRenderer.FixThaiPuaGlyphs("ปด"));
        // ฟ(0E1F) [PUA F702 = sara ii] -> ฟี.
        Assert.Equal("ฟี", PdfSlidesRenderer.FixThaiPuaGlyphs("ฟ"));
        // เจอร(...) [PUA F70E = thanthakhat] -> ...ร์.
        Assert.Equal("ร์", PdfSlidesRenderer.FixThaiPuaGlyphs("ร"));
    }

    [Fact]
    public void FixThaiPuaGlyphs_LeavesOrdinaryTextUntouched()
    {
        const string ordinary = "School Bright โรงเรียน 123";

        Assert.Equal(ordinary, PdfSlidesRenderer.FixThaiPuaGlyphs(ordinary));
    }

    // ---- JoinLinesForNarration ------------------------------------------------

    [Fact]
    public void JoinLinesForNarration_StripsBulletMarkers()
    {
        var result = PdfSlidesRenderer.JoinLinesForNarration("●First point\n○Second point");

        Assert.Equal("First point. Second point.", result);
    }

    [Fact]
    public void JoinLinesForNarration_AddsAFullStopBetweenLines_SoTheyDontRunTogether()
    {
        // Previously (page.Text, no line handling at all) a title glued straight onto the
        // previous bullet's last word with no separator at all.
        var result = PdfSlidesRenderer.JoinLinesForNarration("First bullet\nProblem");

        Assert.Equal("First bullet. Problem.", result);
    }

    [Fact]
    public void JoinLinesForNarration_DoesNotDoubleUpPunctuation_WhenALineAlreadyEndsWithIt()
    {
        var result = PdfSlidesRenderer.JoinLinesForNarration("What is this?\nAn example:");

        Assert.Equal("What is this? An example:", result);
    }

    [Fact]
    public void JoinLinesForNarration_DropsBlankLines()
    {
        var result = PdfSlidesRenderer.JoinLinesForNarration("First\n\n\nSecond");

        Assert.Equal("First. Second.", result);
    }

    [Fact]
    public void JoinLinesForNarration_OfEmptyInput_ReturnsEmptyString()
    {
        Assert.Equal("", PdfSlidesRenderer.JoinLinesForNarration(""));
    }

    // ---- ExtractHeadingCandidate ------------------------------------------------

    [Fact]
    public void ExtractHeadingCandidate_ReturnsFirstNonEmptyLine()
    {
        var result = PdfSlidesRenderer.ExtractHeadingCandidate("\n\nการใช้งานระบบ School Bright\nเนื้อหาบรรทัดถัดไป");

        Assert.Equal("การใช้งานระบบ School Bright", result);
    }

    [Fact]
    public void ExtractHeadingCandidate_StripsLeadingBulletMarker()
    {
        var result = PdfSlidesRenderer.ExtractHeadingCandidate("●บทที่ 1: เริ่มต้นใช้งาน\nรายละเอียด");

        Assert.Equal("บทที่ 1: เริ่มต้นใช้งาน", result);
    }

    [Fact]
    public void ExtractHeadingCandidate_ReturnsNull_WhenFirstLineIsLongerThan120Characters()
    {
        var longLine = new string('a', 121);

        var result = PdfSlidesRenderer.ExtractHeadingCandidate($"{longLine}\nShort heading");

        Assert.Null(result);
    }

    [Fact]
    public void ExtractHeadingCandidate_AcceptsLineAtExactly120Characters()
    {
        var exactLine = new string('a', 120);

        var result = PdfSlidesRenderer.ExtractHeadingCandidate(exactLine);

        Assert.Equal(exactLine, result);
    }

    [Fact]
    public void ExtractHeadingCandidate_ReturnsNull_WhenTextIsOnlyBlankLines()
    {
        Assert.Null(PdfSlidesRenderer.ExtractHeadingCandidate("\n\n   \n"));
    }

    // ---- ResolveTitle ------------------------------------------------

    [Fact]
    public void ResolveTitle_PrefersMetadataTitle_WhenPresent()
    {
        var result = PdfSlidesRenderer.ResolveTitle("Metadata Title", "Heading Candidate", "668832349.pdf");

        Assert.Equal("Metadata Title", result);
    }

    [Fact]
    public void ResolveTitle_FallsBackToHeadingCandidate_WhenMetadataTitleIsMissing()
    {
        Assert.Equal("Heading Candidate", PdfSlidesRenderer.ResolveTitle(null, "Heading Candidate", "668832349.pdf"));
        Assert.Equal("Heading Candidate", PdfSlidesRenderer.ResolveTitle("   ", "Heading Candidate", "668832349.pdf"));
    }

    [Fact]
    public void ResolveTitle_FallsBackToFilename_WhenNeitherMetadataNorHeadingIsUsable()
    {
        Assert.Equal("668832349.pdf", PdfSlidesRenderer.ResolveTitle(null, null, "668832349.pdf"));
        Assert.Equal("668832349.pdf", PdfSlidesRenderer.ResolveTitle("", "   ", "668832349.pdf"));
    }

    [Fact]
    public void ResolveTitle_TrimsWhitespace_FromMetadataTitle()
    {
        Assert.Equal("Trimmed Title", PdfSlidesRenderer.ResolveTitle("  Trimmed Title  ", null, "fallback.pdf"));
    }
}
