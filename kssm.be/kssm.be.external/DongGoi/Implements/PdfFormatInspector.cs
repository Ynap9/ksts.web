using kssm.be.external.DongGoi.Dtos;
using kssm.be.external.DongGoi.Interfaces;
using kssm.be.external.KySo.Pdf.Dtos;
using kssm.be.external.KySo.Pdf.Interfaces;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace kssm.be.external.DongGoi.Implements
{
    public class PdfFormatInspector : IPdfFormatInspector
    {
        private const string PdfAIdNamespace = "http://www.aiim.org/pdfa/ns/id/";
        private const int HeaderProbeBytes = 1024;

        private static readonly Regex MetadataRef = new(@"/Metadata\s+(\d+)\s+\d+\s+R", RegexOptions.Compiled);
        private static readonly Regex PagesRef = new(@"/Pages\s+(\d+)\s+\d+\s+R", RegexOptions.Compiled);
        private static readonly Regex PageCountValue = new(@"/Count\s+(\d+)", RegexOptions.Compiled);
        private static readonly Regex PdfAPart = new(@"pdfaid:part\s*[>=]\s*[""']?\s*(\d+)", RegexOptions.Compiled);
        private static readonly Regex PdfAConformance = new(@"pdfaid:conformance\s*[>=]\s*[""']?\s*([A-Za-z])", RegexOptions.Compiled);
        private static readonly Regex ImageSubtype = new(@"/Subtype\s*/Image\b", RegexOptions.Compiled);
        private static readonly Regex FontType = new(@"/Type\s*/Font\b", RegexOptions.Compiled);
        private static readonly Regex PageType = new(@"/Type\s*/Page\b", RegexOptions.Compiled);

        private readonly IPdfRevisionReader _pdfRevisionReader;

        public PdfFormatInspector(IPdfRevisionReader pdfRevisionReader)
        {
            _pdfRevisionReader = pdfRevisionReader;
        }

        public PdfInspectResultDto Inspect(byte[] bytes)
        {
            var result = new PdfInspectResultDto();

            try
            {
                var dauFile = Encoding.Latin1.GetString(bytes, 0, Math.Min(HeaderProbeBytes, bytes.Length));
                if (!dauFile.Contains("%PDF-", StringComparison.Ordinal))
                {
                    result.Verdict = PdfFormatVerdict.NotPdf;
                    return result;
                }

                var revision = _pdfRevisionReader.Load(bytes);
                var catalog = _pdfRevisionReader.GetObjectBody(revision, revision.RootObjectNumber);

                result.PageCount = DemSoTrang(revision, catalog);
                DocPdfAId(revision, catalog, result);
                DocHaiLop(revision, result);

                if (string.IsNullOrEmpty(result.PdfAPart))
                {
                    result.Verdict = PdfFormatVerdict.NotPdfA;
                    return result;
                }

                result.Verdict = result.HasImageLayer && result.HasTextLayer
                    ? PdfFormatVerdict.Valid
                    : PdfFormatVerdict.NotTwoLayer;
                return result;
            }
            catch (Exception ex)
            {
                result.Verdict = PdfFormatVerdict.ReadFailed;
                result.Reason = ex.Message;
                return result;
            }
        }

        public void DocPdfAId(PdfRevisionDto revision, string? catalog, PdfInspectResultDto result)
        {
            if (catalog == null)
            {
                return;
            }

            var traTo = MetadataRef.Match(catalog);
            if (!traTo.Success)
            {
                return;
            }

            var soHieu = int.Parse(traTo.Groups[1].Value);
            var xmpBytes = _pdfRevisionReader.GetRawStreamBytes(revision, soHieu);
            if (xmpBytes == null)
            {
                return;
            }

            var dict = _pdfRevisionReader.GetObjectBody(revision, soHieu);
            if (dict != null && dict.Contains("/FlateDecode", StringComparison.Ordinal))
            {
                xmpBytes = GiaiNenFlate(xmpBytes) ?? xmpBytes;
            }

            var xmp = Encoding.UTF8.GetString(xmpBytes);
            if (!xmp.Contains(PdfAIdNamespace, StringComparison.Ordinal))
            {
                return;
            }

            var phan = PdfAPart.Match(xmp);
            if (phan.Success)
            {
                result.PdfAPart = phan.Groups[1].Value;
            }

            var mucDo = PdfAConformance.Match(xmp);
            if (mucDo.Success)
            {
                result.PdfAConformance = mucDo.Groups[1].Value.ToUpperInvariant();
            }
        }

        public byte[]? GiaiNenFlate(byte[] raw)
        {
            try
            {
                using var nguon = new MemoryStream(raw);
                using var giaiNen = new ZLibStream(nguon, CompressionMode.Decompress);
                using var dich = new MemoryStream();
                giaiNen.CopyTo(dich);
                return dich.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void DocHaiLop(PdfRevisionDto revision, PdfInspectResultDto result)
        {
            foreach (var soHieu in revision.Objects.Keys.ToList())
            {
                if (result.HasImageLayer && result.HasTextLayer)
                {
                    return;
                }

                var than = _pdfRevisionReader.GetObjectBody(revision, soHieu);
                if (than == null)
                {
                    continue;
                }

                if (!result.HasImageLayer && ImageSubtype.IsMatch(than))
                {
                    result.HasImageLayer = true;
                }

                if (!result.HasTextLayer &&
                    (FontType.IsMatch(than) || than.Contains("/BaseFont", StringComparison.Ordinal)))
                {
                    result.HasTextLayer = true;
                }
            }
        }

        public int DemSoTrang(PdfRevisionDto revision, string? catalog)
        {
            if (catalog != null)
            {
                var traTo = PagesRef.Match(catalog);
                if (traTo.Success)
                {
                    var goc = _pdfRevisionReader.GetObjectBody(revision, int.Parse(traTo.Groups[1].Value));
                    var soLuong = goc == null ? Match.Empty : PageCountValue.Match(goc);
                    if (soLuong.Success)
                    {
                        return int.Parse(soLuong.Groups[1].Value);
                    }
                }
            }

            var dem = 0;
            foreach (var soHieu in revision.Objects.Keys.ToList())
            {
                var than = _pdfRevisionReader.GetObjectBody(revision, soHieu);
                if (than != null && PageType.IsMatch(than))
                {
                    dem++;
                }
            }

            return dem;
        }
    }
}
