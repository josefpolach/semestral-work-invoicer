using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace Invoicing.Pdf;

/// <summary>Registers the licence and bundled Czech-capable fonts.</summary>
public static class PdfSetup
{
    internal const string FontFamily = "Noto Sans";
    private const string FontResources = "Invoicing.Pdf.Resources.Fonts.";
    private static readonly Lazy<bool> _initialized = new(RegisterFonts);

    public static void Initialize() => _ = _initialized.Value;

    private static bool RegisterFonts()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = true;
        var assembly = typeof(PdfSetup).Assembly;
        FontManager.RegisterFontFromEmbeddedResource(assembly, FontResources + "NotoSans-Regular.ttf");
        FontManager.RegisterFontFromEmbeddedResource(assembly, FontResources + "NotoSans-Bold.ttf");
        return true;
    }
}
