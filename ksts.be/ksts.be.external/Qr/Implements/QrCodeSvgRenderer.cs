using ksts.be.external.Qr.Interfaces;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using Net.Codecrete.QrCodeGenerator;
using System.Text;

namespace ksts.be.external.Qr.Implements
{
    /// <summary>
    /// Dựng SVG mã QR, mỗi ô mã là một đơn vị của viewBox. Các ô đen liền nhau trên cùng hàng gộp thành
    /// một hình chữ nhật để chuỗi SVG không phình lên khi in hàng loạt.
    /// </summary>
    public class QrCodeSvgRenderer : IQrCodeSvgRenderer
    {
        /// <summary>Vùng lặng quanh mã, tính theo ô. Chuẩn đòi 4 ô, để 2 vì ô chứa mã đã có nền trắng và khoảng đệm.</summary>
        private const int QuietZoneModules = 2;

        /// <summary>Màu ô đen, lấy đúng màu chủ đạo của giấy báo.</summary>
        private const string ModuleColor = "#0f2547";

        /// <summary>Nền phải đặc, không để trong suốt: lưới phác thảo chạy dưới nền ăn vào vùng lặng làm máy quét đọc nhầm.</summary>
        private const string BackgroundColor = "#ffffff";

        /// <inheritdoc/>
        public string RenderSvg(string noiDung)
        {
            if (string.IsNullOrWhiteSpace(noiDung))
            {
                throw new UserFriendlyException(ErrorCodes.QrContentEmpty,
                    "Không có nội dung để dựng mã QR tra cứu.");
            }

            QrCode qr;
            try
            {
                qr = QrCode.EncodeText(noiDung.Trim(), QrCode.Ecc.Medium);
            }
            catch (DataTooLongException)
            {
                throw new UserFriendlyException(ErrorCodes.QrContentTooLong,
                    "Nội dung quá dài, không dựng được mã QR tra cứu.");
            }

            var canh = qr.Size + QuietZoneModules * 2;
            var path = new StringBuilder();

            for (var y = 0; y < qr.Size; y++)
            {
                var x = 0;
                while (x < qr.Size)
                {
                    if (!qr.GetModule(x, y))
                    {
                        x++;
                        continue;
                    }

                    var batDau = x;
                    while (x < qr.Size && qr.GetModule(x, y))
                    {
                        x++;
                    }

                    var doDai = x - batDau;
                    path.Append('M').Append(batDau + QuietZoneModules)
                        .Append(' ').Append(y + QuietZoneModules)
                        .Append('h').Append(doDai)
                        .Append("v1h-").Append(doDai).Append('z');
                }
            }

            return $"<svg viewBox=\"0 0 {canh} {canh}\" xmlns=\"http://www.w3.org/2000/svg\" "
                 + "shape-rendering=\"crispEdges\" preserveAspectRatio=\"xMidYMid meet\" "
                 + "style=\"display:block;width:100%;height:100%\">"
                 + $"<rect width=\"{canh}\" height=\"{canh}\" fill=\"{BackgroundColor}\"/>"
                 + $"<path fill=\"{ModuleColor}\" d=\"{path}\"/></svg>";
        }
    }
}
