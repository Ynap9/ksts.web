using ClosedXML.Excel;
using ksts.be.external.Excel.Dtos;
using ksts.be.external.Excel.Interfaces;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using System.Globalization;
using System.Text;

namespace ksts.be.external.Excel.Implements
{
    /// <summary>
    /// Đọc file Excel bằng ClosedXML. Mọi giá trị trả về dạng chuỗi đúng như người nhập thấy trong Excel:
    /// điểm số và số CCCD mà quy về số sẽ mất số 0 đứng đầu và mất chữ số thập phân.
    /// </summary>
    public class ExcelSheetReader : IExcelSheetReader
    {
        private const string DateFormat = "dd/MM/yyyy";

        /// <inheritdoc/>
        public List<ExcelSheetInfoDto> ListSheets(Stream stream)
        {
            using var workbook = MoWorkbook(stream);
            return workbook.Worksheets
                .Select(sheet => new ExcelSheetInfoDto
                {
                    Name = sheet.Name,
                    RowCount = Math.Max(0, (sheet.RangeUsed()?.RowCount() ?? 0) - 1)
                })
                .ToList();
        }

        /// <inheritdoc/>
        public ExcelSheetDto ReadSheet(Stream stream, string? sheetName, int startRow)
        {
            using var workbook = MoWorkbook(stream);

            var sheet = string.IsNullOrWhiteSpace(sheetName)
                ? workbook.Worksheets.FirstOrDefault()
                : workbook.Worksheets.FirstOrDefault(x => x.Name == sheetName);

            if (sheet == null)
            {
                throw new UserFriendlyException(ErrorCodes.ExcelUnreadable,
                    string.IsNullOrWhiteSpace(sheetName)
                        ? "File Excel không có sheet nào."
                        : $"Không tìm thấy sheet \"{sheetName}\" trong file.");
            }

            var result = new ExcelSheetDto();
            var dongTieuDe = Math.Max(1, startRow);
            var dongCuoi = sheet.LastRowUsed()?.RowNumber() ?? 0;
            var cotCuoi = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            if (dongCuoi < dongTieuDe || cotCuoi == 0)
            {
                return result;
            }

            var keys = new List<string>();
            for (var c = 1; c <= cotCuoi; c++)
            {
                var tieuDe = sheet.Cell(dongTieuDe, c).GetFormattedString().Trim();
                result.Headers.Add(tieuDe);
                keys.Add(NormalizeKey(tieuDe));
            }

            for (var r = dongTieuDe + 1; r <= dongCuoi; r++)
            {
                var record = new Dictionary<string, string>();

                for (var c = 1; c <= cotCuoi; c++)
                {
                    var khoa = keys[c - 1];
                    if (string.IsNullOrEmpty(khoa))
                    {
                        continue;
                    }

                    var cell = sheet.Cell(r, c);
                    record[khoa] = cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var ngay)
                        ? ngay.ToString(DateFormat, CultureInfo.InvariantCulture)
                        : cell.GetFormattedString().Trim();
                }

                result.Rows.Add(record);
            }

            return result;
        }

        /// <inheritdoc/>
        public string NormalizeKey(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var chuan = text.Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD);
            var ketQua = new StringBuilder(chuan.Length);

            foreach (var kyTu in chuan)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(kyTu) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(kyTu))
                {
                    ketQua.Append(char.ToLowerInvariant(kyTu));
                }
            }

            return ketQua.ToString();
        }

        /// <inheritdoc/>
        public string LayGiaTri(IReadOnlyDictionary<string, string> row, IEnumerable<string> tenCotUngVien)
        {
            foreach (var tenCot in tenCotUngVien)
            {
                if (row.TryGetValue(NormalizeKey(tenCot), out var giaTri) && !string.IsNullOrWhiteSpace(giaTri))
                {
                    return giaTri;
                }
            }

            return string.Empty;
        }

        /// <summary>Mở workbook, quy mọi lỗi định dạng về một thông báo người dùng hiểu được.</summary>
        public XLWorkbook MoWorkbook(Stream stream)
        {
            try
            {
                return new XLWorkbook(stream);
            }
            catch (Exception)
            {
                throw new UserFriendlyException(ErrorCodes.ExcelUnreadable,
                    "Không đọc được file Excel. Chỉ hỗ trợ định dạng .xlsx.");
            }
        }
    }
}
