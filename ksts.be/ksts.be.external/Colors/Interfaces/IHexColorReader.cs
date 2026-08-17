using ksts.be.external.Colors.Dtos;

namespace ksts.be.external.Colors.Interfaces
{
    /// <summary>
    /// Reads the "#RRGGBB" colours the template stores for the signature block and the wet-ink image.
    /// Kept out of the drawing code so the PDF layer and the template service agree on what a stored
    /// colour means — including what an absent one means.
    /// </summary>
    public interface IHexColorReader
    {
        /// <summary>
        /// Parses to channels in the 0..1 range PDF works with. Absent means no colour was picked, which
        /// callers read as "leave the original ink alone" — black is a real colour, not that flag.
        /// </summary>
        RgbColorDto? Read(string? hex);

        /// <summary>
        /// Normalises to upper-case "#RRGGBB" before it reaches the database, so the stored value is always
        /// the exact shape the PDF layer and the colour input on screen expect.
        /// </summary>
        string? Normalize(string? hex);
    }
}
