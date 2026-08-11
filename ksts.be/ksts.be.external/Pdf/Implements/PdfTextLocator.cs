using ksts.be.external.Pdf.Dtos;
using ksts.be.external.Pdf.Interfaces;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using System.Globalization;
using System.Text;
using UglyToad.PdfPig;

namespace ksts.be.external.Pdf.Implements
{
    /// <summary>
    /// Trích chữ kèm toạ độ bằng PdfPig.
    ///
    /// Gom word thành dòng theo BASELINE: word cùng một dòng có cạnh dưới gần bằng nhau, sai khác chỉ do dấu
    /// phụ và chữ có phần thò xuống. Dung sai lấy theo chiều cao chữ chứ không phải một số point cố định -
    /// tài liệu cỡ chữ khác nhau thì ngưỡng gộp cũng phải khác.
    /// </summary>
    public class PdfTextLocator : IPdfTextLocator
    {
        private const double BaselineToleranceRatio = 0.5;

        /// <inheritdoc/>
        public PdfPageTextDto GetPageText(Stream pdfStream, int pageNumber)
        {
            using var document = PdfDocument.Open(pdfStream);
            if (pageNumber < 1 || pageNumber > document.NumberOfPages)
            {
                throw new UserFriendlyException(ErrorCodes.SealAnchorNotFound,
                    $"Tài liệu không có trang {pageNumber} (tổng {document.NumberOfPages} trang).");
            }

            var page = document.GetPage(pageNumber);
            var pageWidth = page.Width;
            var pageHeight = page.Height;

            var result = new PdfPageTextDto
            {
                PageNumber = pageNumber,
                PageCount = document.NumberOfPages,
                WidthPoints = pageWidth,
                HeightPoints = pageHeight,
            };

            if (pageWidth <= 0 || pageHeight <= 0)
            {
                return result;
            }

            var words = page.GetWords()
                .Where(w => !string.IsNullOrWhiteSpace(w.Text))
                .OrderByDescending(w => w.BoundingBox.Bottom)
                .ThenBy(w => w.BoundingBox.Left)
                .ToList();

            var groups = new List<List<UglyToad.PdfPig.Content.Word>>();
            foreach (var word in words)
            {
                var tolerance = Math.Max(word.BoundingBox.Height * BaselineToleranceRatio, 1);
                var group = groups.FirstOrDefault(g =>
                    Math.Abs(g[0].BoundingBox.Bottom - word.BoundingBox.Bottom) <= tolerance);

                if (group == null)
                {
                    groups.Add(new List<UglyToad.PdfPig.Content.Word> { word });
                }
                else
                {
                    group.Add(word);
                }
            }

            foreach (var group in groups)
            {
                var ordered = group.OrderBy(w => w.BoundingBox.Left).ToList();
                var left = ordered.Min(w => w.BoundingBox.Left);
                var right = ordered.Max(w => w.BoundingBox.Right);
                var top = ordered.Max(w => w.BoundingBox.Top);
                var bottom = ordered.Min(w => w.BoundingBox.Bottom);
                var text = string.Join(" ", ordered.Select(w => w.Text));

                result.Lines.Add(new PdfTextLineDto
                {
                    Text = text,
                    NormalizedText = Normalize(text),
                    LeftRatio = left / pageWidth,
                    TopRatio = (pageHeight - top) / pageHeight,
                    WidthRatio = (right - left) / pageWidth,
                    HeightRatio = (top - bottom) / pageHeight,
                });
            }

            result.Lines = result.Lines.OrderBy(l => l.TopRatio).ThenBy(l => l.LeftRatio).ToList();
            return result;
        }

        /// <inheritdoc/>
        public string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var decomposed = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            var lastWasSpace = false;

            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsWhiteSpace(ch))
                {
                    if (!lastWasSpace && builder.Length > 0)
                    {
                        builder.Append(' ');
                        lastWasSpace = true;
                    }
                    continue;
                }

                var upper = char.ToUpperInvariant(ch);
                builder.Append(upper == 'Đ' ? 'D' : upper);
                lastWasSpace = false;
            }

            return builder.ToString().TrimEnd();
        }
    }
}
