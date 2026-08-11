using ksts.be.external.Images.Dtos;
using ksts.be.external.Pdf.Dtos;
using ksts.be.external.Pdf.Interfaces;
using ksts.be.external.Placement.Dtos;
using ksts.be.external.Placement.Interfaces;
using ksts.be.shared.Constants.Signing;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using Microsoft.Extensions.Logging;

namespace ksts.be.external.Placement.Implements
{
    /// <inheritdoc/>
    public class SealPlacementResolver : ISealPlacementResolver
    {
        private readonly IPdfTextLocator _textLocator;
        private readonly ILogger<SealPlacementResolver> _logger;

        public SealPlacementResolver(IPdfTextLocator textLocator, ILogger<SealPlacementResolver> logger)
        {
            _textLocator = textLocator;
            _logger = logger;
        }

        /// <inheritdoc/>
        public SealPlacementResultDto Resolve(Stream pdfStream, int pageNumber, ImageSizeDto? dauDo,
            ImageSizeDto? chuKyTuoi)
        {
            _logger.LogInformation($"{nameof(Resolve)} pageNumber={pageNumber}");

            var page = _textLocator.GetPageText(pdfStream, pageNumber);

            var anchorTop = FindAnchorChucDanh(page.Lines)
                ?? throw new UserFriendlyException(ErrorCodes.SealAnchorNotFound,
                    "Không tìm thấy dòng chức danh người ký trên tài liệu nên chưa xác định được chỗ đóng dấu.");

            var anchorBottom = FindAnchorTenNguoiKy(page.Lines, anchorTop)
                ?? throw new UserFriendlyException(ErrorCodes.SealAnchorNotFound,
                    $"Tìm thấy \"{anchorTop.Text}\" nhưng không thấy dòng tên người ký bên dưới.");

            var result = new SealPlacementResultDto
            {
                PageNumber = pageNumber,
                PageWidthPoints = page.WidthPoints,
                PageHeightPoints = page.HeightPoints,
                AnchorChucDanh = anchorTop.Text,
                AnchorTenNguoiKy = anchorBottom.Text,
                MidXRatio = (anchorTop.CenterXRatio + anchorBottom.CenterXRatio) / 2,
                MidYRatio = (anchorTop.CenterYRatio + anchorBottom.CenterYRatio) / 2,
            };

            if (dauDo != null)
            {
                result.DauDo = BuildCenteredRect(result.MidXRatio, result.MidYRatio,
                    dauDo.WidthPoints, dauDo.HeightPoints, page.WidthPoints, page.HeightPoints);

                if (!dauDo.HasDpiMetadata)
                {
                    result.CanhBao = $"Ảnh dấu đỏ không khai độ phân giải, đang tạm tính theo "
                        + $"{SealPlacementConstants.DefaultImageDpi} DPI. Nếu dấu ra sai cỡ thì cần quét lại ảnh đúng DPI.";
                }
            }

            if (chuKyTuoi != null)
            {
                var widthPoints = chuKyTuoi.WidthPoints;
                var heightPoints = chuKyTuoi.HeightPoints;
                var maxWidthPoints = page.WidthPoints * SealPlacementConstants.MaxChuKyTuoiWidthRatio;

                if (widthPoints > maxWidthPoints && widthPoints > 0)
                {
                    heightPoints *= maxWidthPoints / widthPoints;
                    widthPoints = maxWidthPoints;
                }

                result.ChuKyTuoi = BuildCenteredRect(result.MidXRatio, result.MidYRatio,
                    widthPoints, heightPoints, page.WidthPoints, page.HeightPoints);
            }

            return result;
        }

        /// <inheritdoc/>
        public PdfTextLineDto? FindAnchorChucDanh(IReadOnlyList<PdfTextLineDto> lines)
        {
            foreach (var anchor in SealPlacementConstants.AnchorChucDanh)
            {
                var matched = lines.FirstOrDefault(l => l.NormalizedText.Contains(anchor, StringComparison.Ordinal));
                if (matched != null)
                {
                    return matched;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public PdfTextLineDto? FindAnchorTenNguoiKy(IReadOnlyList<PdfTextLineDto> lines,
            PdfTextLineDto anchorChucDanh)
        {
            var below = lines
                .Where(l => l.TopRatio > anchorChucDanh.TopRatio)
                .OrderBy(l => l.TopRatio)
                .Take(SealPlacementConstants.AnchorMaxLinesBelow)
                .ToList();

            foreach (var anchor in SealPlacementConstants.AnchorTenNguoiKy)
            {
                var matched = below.FirstOrDefault(l => l.NormalizedText.Contains(anchor, StringComparison.Ordinal));
                if (matched != null)
                {
                    return matched;
                }
            }

            var aligned = below
                .Where(l => Math.Abs(l.CenterXRatio - anchorChucDanh.CenterXRatio)
                    <= SealPlacementConstants.AnchorAlignTolerance)
                .ToList();

            var byHocVi = aligned.FirstOrDefault(l => SealPlacementConstants.AnchorHocViPrefixes
                .Any(prefix => l.NormalizedText.StartsWith(prefix, StringComparison.Ordinal)));

            return byHocVi ?? aligned.LastOrDefault();
        }

        /// <inheritdoc/>
        public PdfRectRatioDto BuildCenteredRect(double centerXRatio, double centerYRatio, double widthPoints,
            double heightPoints, double pageWidthPoints, double pageHeightPoints)
        {
            if (pageWidthPoints <= 0 || pageHeightPoints <= 0)
            {
                throw new UserFriendlyException(ErrorCodes.SealAnchorNotFound,
                    "Không đọc được khổ trang PDF nên không tính được vị trí đặt dấu.");
            }

            var widthRatio = widthPoints / pageWidthPoints;
            var heightRatio = heightPoints / pageHeightPoints;
            var marginXRatio = SealPlacementConstants.PageMargin / pageWidthPoints;
            var marginYRatio = SealPlacementConstants.PageMargin / pageHeightPoints;

            var left = centerXRatio - widthRatio / 2;
            var top = centerYRatio - heightRatio / 2;

            left = widthRatio + 2 * marginXRatio >= 1
                ? (1 - widthRatio) / 2
                : Math.Clamp(left, marginXRatio, 1 - marginXRatio - widthRatio);

            top = heightRatio + 2 * marginYRatio >= 1
                ? (1 - heightRatio) / 2
                : Math.Clamp(top, marginYRatio, 1 - marginYRatio - heightRatio);

            return new PdfRectRatioDto
            {
                XRatio = left,
                YRatio = top,
                WidthRatio = widthRatio,
                HeightRatio = heightRatio,
            };
        }
    }
}
