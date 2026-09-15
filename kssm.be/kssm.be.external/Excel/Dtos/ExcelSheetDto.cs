namespace kssm.be.external.Excel.Dtos
{
    public class ExcelSheetDto
    {
        public List<string> Headers { get; set; } = new();

        public List<Dictionary<string, string>> Rows { get; set; } = new();
    }
}
