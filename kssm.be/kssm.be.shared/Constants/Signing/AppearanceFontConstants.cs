namespace kssm.be.shared.Constants.Signing
{
    public static class AppearanceFontConstants
    {
        public static readonly IReadOnlyList<string> SearchDirectories = new[]
        {
            "/usr/share/fonts",
            "/usr/local/share/fonts",
        };

        public static readonly IReadOnlyList<string> RegularFiles = new[]
        {
            "times.ttf",
            "LiberationSerif-Regular.ttf",
            "Tinos-Regular.ttf",
            "DejaVuSerif.ttf",
            "NotoSerif-Regular.ttf",
            "FreeSerif.ttf",
        };

        public static readonly IReadOnlyList<string> BoldFiles = new[]
        {
            "timesbd.ttf",
            "LiberationSerif-Bold.ttf",
            "Tinos-Bold.ttf",
            "DejaVuSerif-Bold.ttf",
            "NotoSerif-Bold.ttf",
            "FreeSerifBold.ttf",
        };

        public static readonly IReadOnlyList<string> ItalicFiles = new[]
        {
            "timesi.ttf",
            "LiberationSerif-Italic.ttf",
            "Tinos-Italic.ttf",
            "DejaVuSerif-Italic.ttf",
            "NotoSerif-Italic.ttf",
            "FreeSerifItalic.ttf",
        };

        public static readonly IReadOnlyList<string> BoldItalicFiles = new[]
        {
            "timesbi.ttf",
            "LiberationSerif-BoldItalic.ttf",
            "Tinos-BoldItalic.ttf",
            "DejaVuSerif-BoldItalic.ttf",
            "NotoSerif-BoldItalic.ttf",
            "FreeSerifBoldItalic.ttf",
        };
    }
}
