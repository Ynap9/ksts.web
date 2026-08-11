using ksts.be.external.Images.Dtos;
using ksts.be.external.Images.Interfaces;
using ksts.be.shared.Constants.Signing;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using System.Buffers.Binary;

namespace ksts.be.external.Images.Implements
{
    /// <summary>
    /// Đọc kích thước và DPI của PNG / JPEG bằng cách phân tích header.
    ///
    /// Quy đổi sang point theo công thức của hệ toạ độ PDF: point = pixel / dpi * 72.
    /// </summary>
    public class ImageSizeReader : IImageSizeReader
    {
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <inheritdoc/>
        public ImageSizeDto Read(byte[] bytes)
        {
            var size = TryReadPng(bytes) ?? TryReadJpeg(bytes);
            if (size == null)
            {
                throw new UserFriendlyException(ErrorCodes.SealImageUnreadable,
                    "Không đọc được kích thước ảnh. Chỉ hỗ trợ PNG và JPEG.");
            }

            if (!size.HasDpiMetadata)
            {
                size.DpiX = SealPlacementConstants.DefaultImageDpi;
                size.DpiY = SealPlacementConstants.DefaultImageDpi;
            }

            size.WidthPoints = size.PixelWidth / size.DpiX * SealPlacementConstants.PointsPerInch;
            size.HeightPoints = size.PixelHeight / size.DpiY * SealPlacementConstants.PointsPerInch;
            return size;
        }

        /// <inheritdoc/>
        public ImageSizeDto? TryReadPng(byte[] bytes)
        {
            if (bytes.Length < 24 || !bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
            {
                return null;
            }

            var result = new ImageSizeDto
            {
                PixelWidth = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)),
                PixelHeight = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)),
            };

            var offset = 8;
            while (offset + 8 <= bytes.Length)
            {
                var length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
                var type = System.Text.Encoding.ASCII.GetString(bytes, offset + 4, 4);
                var dataStart = offset + 8;

                if (length < 0 || dataStart + length > bytes.Length)
                {
                    break;
                }

                if (type == "pHYs" && length >= 9)
                {
                    var ppuX = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(dataStart, 4));
                    var ppuY = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(dataStart + 4, 4));
                    var unit = bytes[dataStart + 8];
                    if (unit == 1 && ppuX > 0 && ppuY > 0)
                    {
                        result.DpiX = ppuX * 0.0254;
                        result.DpiY = ppuY * 0.0254;
                        result.HasDpiMetadata = true;
                    }
                    break;
                }

                if (type == "IDAT" || type == "IEND")
                {
                    break;
                }

                offset = dataStart + length + 4;
            }

            return result.PixelWidth > 0 && result.PixelHeight > 0 ? result : null;
        }

        /// <inheritdoc/>
        public ImageSizeDto? TryReadJpeg(byte[] bytes)
        {
            if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
            {
                return null;
            }

            var result = new ImageSizeDto();
            var offset = 2;

            while (offset + 4 <= bytes.Length)
            {
                if (bytes[offset] != 0xFF)
                {
                    offset++;
                    continue;
                }

                var marker = bytes[offset + 1];
                if (marker == 0xD8 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
                {
                    offset += 2;
                    continue;
                }
                if (marker == 0xD9 || marker == 0xDA)
                {
                    break;
                }

                var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 2, 2));
                var dataStart = offset + 4;
                if (segmentLength < 2 || dataStart + segmentLength - 2 > bytes.Length)
                {
                    break;
                }

                if (marker == 0xE0 && segmentLength >= 16
                    && System.Text.Encoding.ASCII.GetString(bytes, dataStart, 4) == "JFIF")
                {
                    var units = bytes[dataStart + 7];
                    var densityX = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(dataStart + 8, 2));
                    var densityY = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(dataStart + 10, 2));
                    if (densityX > 0 && densityY > 0 && (units == 1 || units == 2))
                    {
                        result.DpiX = units == 1 ? densityX : densityX * 2.54;
                        result.DpiY = units == 1 ? densityY : densityY * 2.54;
                        result.HasDpiMetadata = true;
                    }
                }

                var isStartOfFrame = marker >= 0xC0 && marker <= 0xCF
                    && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
                if (isStartOfFrame && segmentLength >= 7)
                {
                    result.PixelHeight = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(dataStart + 1, 2));
                    result.PixelWidth = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(dataStart + 3, 2));
                    break;
                }

                offset = dataStart + segmentLength - 2;
            }

            return result.PixelWidth > 0 && result.PixelHeight > 0 ? result : null;
        }
    }
}
