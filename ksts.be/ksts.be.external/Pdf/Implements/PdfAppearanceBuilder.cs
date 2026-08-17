using ksts.be.external.Colors.Dtos;
using ksts.be.external.Colors.Interfaces;
using ksts.be.external.Pdf.Dtos;
using ksts.be.external.Pdf.Interfaces;
using ksts.be.shared.Constants.Signing;
using ksts.be.shared.Constants.Template;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ksts.be.external.Pdf.Implements
{
    /// <inheritdoc/>
    public class PdfAppearanceBuilder : IPdfAppearanceBuilder
    {
        private readonly IPdfRevisionReader _revisionReader;
        private readonly IHexColorReader _hexColorReader;

        /// <summary>
        /// Khoá quanh phần dùng PDFsharp. Nhiều file ký song song, mà PDFsharp giữ bộ nhớ đệm font dùng chung
        /// cho cả tiến trình — vẽ đồng thời là rủi ro thật, trong khi vẽ chỉ tốn ~5 ms nên xếp hàng ở đây
        /// không phải là nút thắt: nút thắt nằm ở MinIO và TSA, hai chỗ vẫn chạy song song bình thường.
        /// </summary>
        private static readonly object _khoaPdfSharp = new();

        public PdfAppearanceBuilder(IPdfRevisionReader revisionReader, IHexColorReader hexColorReader)
        {
            _revisionReader = revisionReader;
            _hexColorReader = hexColorReader;

            // PDFsharp 6 không tự tìm font hệ thống; thiếu cờ này thì vẽ tên người ký ném lỗi thiếu font.
            // Cờ là biến toàn cục của thư viện, đặt lại nhiều lần vô hại.
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }

        /// <inheritdoc/>
        public PdfAppearanceDto BuildText(string dong1, string dong2, int firstObjectNumber, double width,
            double height, string mau)
        {
            // Tỉ lệ lấy theo CHIỀU CAO: hai dòng chữ xếp dọc nên chiều cao mới là chiều quyết định cỡ chữ;
            // lấy theo bề rộng thì ô rộng-thấp sẽ có chữ cao hơn cả hộp.
            var scale = height / SigningConstants.AppearanceHeight;
            var mauChu = _hexColorReader.Read(mau);

            var tempPdf = Draw(width, height, (gfx, box) =>
            {
                var font = new XFont(SigningConstants.AppearanceFontFamily,
                    SigningConstants.AppearanceFontSize * scale);
                var but = new XSolidBrush(mauChu == null
                    ? XColors.Black
                    : XColor.FromArgb(
                        (int)Math.Round(mauChu.Red * 255),
                        (int)Math.Round(mauChu.Green * 255),
                        (int)Math.Round(mauChu.Blue * 255)));

                gfx.DrawString(dong1, font, but,
                    new XRect(0, 0, box.Width, box.Height / 2), XStringFormats.Center);
                gfx.DrawString(dong2, font, but,
                    new XRect(0, box.Height / 2, box.Width, box.Height / 2), XStringFormats.Center);
            });

            return Transplant(tempPdf, firstObjectNumber, width, height);
        }

        /// <inheritdoc/>
        public PdfAppearanceDto BuildImage(byte[] anh, int doDamPhanTram, int doDayNetPhanTram,
            int firstObjectNumber, double width, double height, string? mau)
        {
            if (anh.Length == 0)
            {
                throw new UserFriendlyException(ErrorCodes.AppearanceBuildFailed,
                    "Ảnh chữ ký tươi rỗng nên không dựng được mặt chữ ký.");
            }

            // Stream phải sống tới khi tài liệu tạm được lưu xong: PDFsharp còn đọc lại dữ liệu ảnh ở khâu
            // Save, đóng stream ngay trong lúc vẽ là mất ảnh.
            using var stream = new MemoryStream(anh);
            var tempPdf = Draw(width, height, (gfx, box) =>
            {
                // Một XImage duy nhất cho mọi lớp: PDFsharp gom theo instance nên tám lớp nong nét vẫn chỉ
                // nhúng một ảnh. Dựng XImage mới cho từng lớp là nhân bản byte ảnh lên chín lần.
                var image = XImage.FromStream(stream);
                foreach (var lop in TinhLopNongNet(box.Width, box.Height, doDayNetPhanTram))
                {
                    gfx.DrawImage(image, new XRect(lop.X, lop.Y, lop.Width, lop.Height));
                }
            });

            var appearance = Transplant(tempPdf, firstObjectNumber, width, height);
            ApDoDamVaMau(appearance, doDamPhanTram, mau);
            return appearance;
        }

        /// <inheritdoc/>
        public IReadOnlyList<PdfRectPointsDto> TinhLopNongNet(double width, double height,
            int doDayNetPhanTram)
        {
            var banKinh = width * TemplateConstants.DoDayNetBanKinhTiLe * doDayNetPhanTram / 100d;
            var loi = new PdfRectPointsDto { X = 0, Y = 0, Width = width, Height = height };

            // Ô quá nhỏ so với bán kính thì thu lõi vào sẽ chẳng còn gì để vẽ: giữ nguyên một lớp trải kín ô.
            if (banKinh <= 0 || width <= banKinh * 2 || height <= banKinh * 2)
            {
                return new[] { loi };
            }

            loi = new PdfRectPointsDto
            {
                X = banKinh,
                Y = banKinh,
                Width = width - banKinh * 2,
                Height = height - banKinh * 2,
            };

            var lop = new List<PdfRectPointsDto>();
            for (var i = 0; i < TemplateConstants.DoDayNetSoHuong; i++)
            {
                var goc = 2 * Math.PI * i / TemplateConstants.DoDayNetSoHuong;
                lop.Add(new PdfRectPointsDto
                {
                    X = loi.X + banKinh * Math.Cos(goc),
                    Y = loi.Y + banKinh * Math.Sin(goc),
                    Width = loi.Width,
                    Height = loi.Height,
                });
            }

            lop.Add(loi);
            return lop;
        }

        /// <inheritdoc/>
        public byte[] Draw(double width, double height, Action<XGraphics, XRect> ve)
        {
            lock (_khoaPdfSharp)
            {
                return VeVaoTaiLieuTam(width, height, ve);
            }
        }

        /// <inheritdoc/>
        public byte[] VeVaoTaiLieuTam(double width, double height, Action<XGraphics, XRect> ve)
        {
            using var document = new PdfDocument();
            document.Options.CompressContentStreams = false;

            var page = document.AddPage();
            page.Width = XUnit.FromPoint(width);
            page.Height = XUnit.FromPoint(height);

            using (var gfx = XGraphics.FromPdfPage(page))
            {
                ve(gfx, new XRect(0, 0, width, height));
            }

            using var stream = new MemoryStream();
            document.Save(stream, closeStream: false);
            return stream.ToArray();
        }

        /// <inheritdoc/>
        public PdfAppearanceDto Transplant(byte[] tempPdf, int firstObjectNumber, double width, double height)
        {
            var temp = _revisionReader.Load(tempPdf);

            var catalog = _revisionReader.GetObjectBody(temp, temp.RootObjectNumber)
                ?? throw new UserFriendlyException(ErrorCodes.AppearanceBuildFailed,
                    "Không đọc được Catalog của tài liệu khối chữ ký tạm.");

            var pagesNo = Regex.Match(catalog, @"/Pages\s+(\d+)\s+\d+\s+R");
            var pagesBody = pagesNo.Success
                ? _revisionReader.GetObjectBody(temp, int.Parse(pagesNo.Groups[1].Value))
                : null;
            var kid = Regex.Match(pagesBody ?? string.Empty, @"/Kids\s*\[\s*(\d+)");
            var pageBody = kid.Success
                ? _revisionReader.GetObjectBody(temp, int.Parse(kid.Groups[1].Value))
                : null;

            if (pageBody == null)
            {
                throw new UserFriendlyException(ErrorCodes.AppearanceBuildFailed,
                    "Không đọc được trang của tài liệu khối chữ ký tạm.");
            }

            var contentsMatch = Regex.Match(pageBody, @"/Contents\s+(\d+)\s+\d+\s+R");
            if (!contentsMatch.Success)
            {
                throw new UserFriendlyException(ErrorCodes.AppearanceBuildFailed,
                    "Trang khối chữ ký tạm không có luồng nội dung.");
            }

            var contentNo = int.Parse(contentsMatch.Groups[1].Value);
            var contentDict = _revisionReader.GetObjectBody(temp, contentNo) ?? "<<>>";
            var contentData = _revisionReader.GetRawStreamBytes(temp, contentNo) ?? Array.Empty<byte>();
            var resourcesRaw = ReadResources(temp, pageBody);

            // Gom mọi object mà cây tài nguyên đụng tới: font -> font descriptor -> file font nhúng.
            // Duyệt theo chiều rộng, mỗi object chỉ lấy một lần vì cùng một font dùng cho cả hai dòng chữ.
            var order = new List<int>();
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            foreach (var reference in FindRefs(resourcesRaw)) queue.Enqueue(reference);

            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                if (!visited.Add(n)) continue;

                var body = _revisionReader.GetObjectBody(temp, n);
                if (body == null) continue;

                order.Add(n);
                foreach (var reference in FindRefs(body))
                {
                    if (!visited.Contains(reference)) queue.Enqueue(reference);
                }
            }

            var map = new Dictionary<int, int>();
            var next = firstObjectNumber + 1;
            foreach (var n in order) map[n] = next++;

            var result = new PdfAppearanceDto { FormObjectNumber = firstObjectNumber };

            // Form XObject = luồng nội dung của trang tạm khoác thêm vỏ XObject. BBox đúng bằng khổ khối chữ
            // ký nên nội dung nằm khít, không cần /Matrix.
            var formDict = new StringBuilder();
            formDict.Append("<</Type /XObject/Subtype /Form/FormType 1/BBox [0 0 ")
                .Append(width.ToString("0.####", CultureInfo.InvariantCulture)).Append(' ')
                .Append(height.ToString("0.####", CultureInfo.InvariantCulture))
                .Append("]/Resources ").Append(Renumber(resourcesRaw, map));

            // Giữ nguyên /Filter của luồng nội dung gốc vì ta chép nguyên byte đã mã hoá.
            var filter = Regex.Match(contentDict, @"/Filter\s*(/\w+|\[[^\]]*\])");
            if (filter.Success) formDict.Append("/Filter ").Append(filter.Groups[1].Value);
            formDict.Append("/Length ").Append(contentData.Length).Append(">>");

            result.Objects.Add(new PdfAppearanceObjectDto
            {
                Number = firstObjectNumber,
                DictText = formDict.ToString(),
                StreamData = contentData,
            });

            foreach (var n in order)
            {
                var body = _revisionReader.GetObjectBody(temp, n)!;
                var data = _revisionReader.GetRawStreamBytes(temp, n);
                var dict = Renumber(body, map);

                // /Length phải khớp đúng số byte thực chép sang, không tin số cũ.
                if (data != null)
                {
                    dict = Regex.IsMatch(dict, @"/Length\s+\d+")
                        ? Regex.Replace(dict, @"/Length\s+\d+", "/Length " + data.Length)
                        : dict[..dict.LastIndexOf(">>", StringComparison.Ordinal)]
                            + "/Length " + data.Length + ">>";
                }

                result.Objects.Add(new PdfAppearanceObjectDto
                {
                    Number = map[n],
                    DictText = dict,
                    StreamData = data,
                });
            }

            return result;
        }

        /// <inheritdoc/>
        public string ReadResources(PdfRevisionDto temp, string pageBody)
        {
            var at = pageBody.IndexOf("/Resources", StringComparison.Ordinal);
            if (at < 0) return "<<>>";

            var p = at + "/Resources".Length;
            while (p < pageBody.Length && char.IsWhiteSpace(pageBody[p])) p++;

            if (p < pageBody.Length && pageBody[p] == '<')
            {
                return _revisionReader.ExtractDictionary(pageBody, p);
            }

            var reference = Regex.Match(pageBody[p..], @"^(\d+)\s+\d+\s+R");
            return reference.Success
                ? _revisionReader.GetObjectBody(temp, int.Parse(reference.Groups[1].Value)) ?? "<<>>"
                : "<<>>";
        }

        /// <inheritdoc/>
        public void ApDoDamVaMau(PdfAppearanceDto appearance, int doDamPhanTram, string? mau)
        {
            var heSo = doDamPhanTram / 100d;
            var mauMuc = _hexColorReader.Read(mau);

            // Độ đậm bằng 100% và chưa chọn màu nghĩa là không phải sửa gì: ảnh giữ đúng từng byte bản gốc.
            if (Math.Abs(heSo - 1d) < 0.001 && mauMuc == null)
            {
                return;
            }

            // Lớp mặt nạ trong suốt (/SMask) cũng là một ảnh, nhưng nó tả ĐỘ MỜ chứ không tả màu — kéo tương
            // phản lên đó là ăn mòn viền chữ ký. Gom số hiệu các mặt nạ lại để chừa ra.
            var matNa = new HashSet<int>();
            foreach (var obj in appearance.Objects)
            {
                foreach (Match match in Regex.Matches(obj.DictText, @"/SMask\s+(\d+)\s+\d+\s+R"))
                {
                    matNa.Add(int.Parse(match.Groups[1].Value));
                }
            }

            // Hai vế gộp làm một phép tuyến tính: tăng tương phản rồi hạ độ sáng.
            //   sau tương phản: heSo * x + (1 - heSo) / 2
            //   sau hạ sáng   : doSang * (heSo * x + (1 - heSo) / 2)
            // Khai ra /Decode [d0 d1] với d0 là giá trị tại x = 0 và d1 tại x = 1.
            var doSang = Math.Max(TemplateConstants.DoDamSangToiThieu,
                1 - (heSo - 1) * TemplateConstants.DoDamHeSoGiamSang);

            var d0 = doSang * (1 - heSo) / 2;
            var d1 = d0 + doSang * heSo;

            foreach (var obj in appearance.Objects)
            {
                if (matNa.Contains(obj.Number)
                    || !Regex.IsMatch(obj.DictText, @"/Subtype\s*/Image")
                    || Regex.IsMatch(obj.DictText, @"/ImageMask\s+true")
                    || Regex.IsMatch(obj.DictText, @"/Decode\s*\["))
                {
                    continue;
                }

                var soThanhPhan = DemThanhPhanMau(obj.DictText);
                if (soThanhPhan == 0)
                {
                    continue;
                }

                var soBit = ReadBitsPerComponent(obj.DictText);

                // Ảnh thang xám muốn ra MÀU thì phải đổi hẳn không gian màu sang bảng màu: /Decode một kênh
                // chỉ ánh xạ được sắc xám. Bảng màu đã gánh cả độ đậm nên tuyệt đối không thêm /Decode nữa —
                // với /Indexed thì /Decode đánh vào CHỈ SỐ ô màu, sai một cái là loạn màu cả ảnh.
                if (soThanhPhan == 1 && mauMuc != null && soBit <= 8)
                {
                    var hival = (1 << soBit) - 1;
                    obj.DictText = Regex.Replace(obj.DictText, @"/ColorSpace\s*/DeviceGray",
                        "/ColorSpace " + BuildIndexedPalette(d0, d1, hival, mauMuc));
                    continue;
                }

                var decode = BuildDecodeArray(d0, d1, soThanhPhan, mauMuc);
                var dong = obj.DictText.LastIndexOf(">>", StringComparison.Ordinal);
                obj.DictText = obj.DictText[..dong] + decode + obj.DictText[dong..];
            }
        }

        /// <inheritdoc/>
        public string BuildDecodeArray(double can, double tran, int soThanhPhan, RgbColorDto? mau)
        {
            // Ảnh CMYK: 0 là KHÔNG mực chứ không phải điểm tối, nhuộm theo cùng công thức sẽ ra ảnh âm bản.
            // Nên ở đó chỉ giữ phần độ đậm, đúng như trước khi có tuỳ chọn màu.
            var nhuom = mau != null && soThanhPhan == 3;
            var kenh = new[] { mau?.Red ?? 0d, mau?.Green ?? 0d, mau?.Blue ?? 0d };
            var moc = new List<string>();

            for (var i = 0; i < soThanhPhan; i++)
            {
                // Điểm tối kéo về màu đã chọn, điểm sáng vẫn là trắng: c + (1 - c) * x. Chưa chọn màu thì
                // c = 0 nên công thức rút gọn về đúng phần độ đậm, ảnh giữ nguyên sắc mực gốc.
                var c = nhuom ? kenh[i] : 0d;
                moc.Add(FormatColorValue(c + (1 - c) * can));
                moc.Add(FormatColorValue(c + (1 - c) * tran));
            }

            return "/Decode [" + string.Join(" ", moc) + "]";
        }

        /// <inheritdoc/>
        public string BuildIndexedPalette(double can, double tran, int hival, RgbColorDto mau)
        {
            var bang = new StringBuilder();
            for (var i = 0; i <= hival; i++)
            {
                var xam = (double)i / hival;
                var sauDoDam = Math.Clamp(can + xam * (tran - can), 0d, 1d);

                foreach (var c in new[] { mau.Red, mau.Green, mau.Blue })
                {
                    var giaTri = Math.Clamp(c + (1 - c) * sauDoDam, 0d, 1d);
                    bang.Append(((int)Math.Round(giaTri * 255)).ToString("X2", CultureInfo.InvariantCulture));
                }
            }

            return $"[/Indexed /DeviceRGB {hival} <{bang}>]";
        }

        /// <inheritdoc/>
        public int ReadBitsPerComponent(string dictText)
        {
            var match = Regex.Match(dictText, @"/BitsPerComponent\s+(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 8;
        }

        /// <inheritdoc/>
        public string FormatColorValue(double giaTri) =>
            Math.Clamp(giaTri, 0d, 1d).ToString("0.####", CultureInfo.InvariantCulture);

        /// <inheritdoc/>
        public int DemThanhPhanMau(string dictText)
        {
            if (Regex.IsMatch(dictText, @"/ColorSpace\s*/DeviceRGB")) return 3;
            if (Regex.IsMatch(dictText, @"/ColorSpace\s*/DeviceGray")) return 1;
            if (Regex.IsMatch(dictText, @"/ColorSpace\s*/DeviceCMYK")) return 4;
            return 0;
        }

        /// <inheritdoc/>
        public IEnumerable<int> FindRefs(string dictRaw)
        {
            foreach (Match match in Regex.Matches(dictRaw, @"(\d+)\s+\d+\s+R\b"))
            {
                yield return int.Parse(match.Groups[1].Value);
            }
        }

        /// <inheritdoc/>
        public string Renumber(string dictRaw, IReadOnlyDictionary<int, int> map)
        {
            return Regex.Replace(dictRaw, @"(\d+)(\s+\d+\s+R\b)", match =>
            {
                var n = int.Parse(match.Groups[1].Value);
                return (map.TryGetValue(n, out var target) ? target : n) + match.Groups[2].Value;
            });
        }
    }
}
