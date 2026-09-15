using ClosedXML.Excel;
using kssm.be.external.Excel.Dtos;

namespace kssm.be.external.Excel.Interfaces
{
    public interface IExcelSheetReader
    {
        List<ExcelSheetInfoDto> ListSheets(Stream stream);

        ExcelSheetDto ReadSheet(Stream stream, string? sheetName, int startRow);

        string NormalizeKey(string? text);

        string LayGiaTri(IReadOnlyDictionary<string, string> row, IEnumerable<string> tenCotUngVien);

        XLWorkbook MoWorkbook(Stream stream);
    }
}
