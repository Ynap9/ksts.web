using HtmlAgilityPack;
using ksts.be.external.Html.Interfaces;

namespace ksts.be.external.Html.Implements
{
    /// <summary>
    /// Nhồi dữ liệu vào HTML bằng HtmlAgilityPack. Giá trị chữ được mã hoá thực thể trước khi ghi, nên dấu
    /// &amp; hay &lt; trong tên ngành không phá cấu trúc thẻ.
    /// </summary>
    public class HtmlDocumentFiller : IHtmlDocumentFiller
    {
        /// <inheritdoc/>
        public string Fill(string html, IReadOnlyDictionary<string, string> giaTriTheoId,
            IReadOnlyDictionary<string, string> htmlTheoId)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            foreach (var muc in giaTriTheoId)
            {
                var node = document.GetElementbyId(muc.Key);
                if (node != null)
                {
                    node.InnerHtml = HtmlEntity.Entitize(muc.Value ?? string.Empty, useNames: false);
                }
            }

            foreach (var muc in htmlTheoId)
            {
                var node = document.GetElementbyId(muc.Key);
                if (node != null)
                {
                    node.InnerHtml = muc.Value ?? string.Empty;
                }
            }

            using var writer = new StringWriter();
            document.Save(writer);
            return writer.ToString();
        }
    }
}
