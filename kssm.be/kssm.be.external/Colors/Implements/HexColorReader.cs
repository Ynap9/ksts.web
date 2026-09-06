using kssm.be.external.Colors.Dtos;
using kssm.be.external.Colors.Interfaces;
using kssm.be.shared.Constants.Template;
using System.Globalization;
using System.Text.RegularExpressions;

namespace kssm.be.external.Colors.Implements
{
    public class HexColorReader : IHexColorReader
    {
        public RgbColorDto? Read(string? hex)
        {
            var chuan = Normalize(hex);
            if (chuan == null)
            {
                return null;
            }

            return new RgbColorDto
            {
                Red = int.Parse(chuan.Substring(1, 2), NumberStyles.HexNumber) / 255d,
                Green = int.Parse(chuan.Substring(3, 2), NumberStyles.HexNumber) / 255d,
                Blue = int.Parse(chuan.Substring(5, 2), NumberStyles.HexNumber) / 255d,
            };
        }

        public string? Normalize(string? hex)
        {
            var giaTri = (hex ?? string.Empty).Trim();
            if (giaTri.Length == 0)
            {
                return null;
            }

            if (!giaTri.StartsWith('#'))
            {
                giaTri = "#" + giaTri;
            }

            // Falls back to "not chosen" instead of throwing: the value comes from a colour input, so a bad
            // one means the client sent something odd — failing the whole save over a display colour is too
            // heavy handed.
            return Regex.IsMatch(giaTri, TemplateConstants.MauPattern) ? giaTri.ToUpperInvariant() : null;
        }
    }
}
