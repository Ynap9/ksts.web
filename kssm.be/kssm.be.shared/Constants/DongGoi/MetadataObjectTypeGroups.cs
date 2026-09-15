namespace kssm.be.shared.Constants.DongGoi
{
    public static class MetadataObjectTypeGroups
    {
        public static readonly IReadOnlyList<MetadataObjectType> CapGoi = new[]
        {
            MetadataObjectType.Dossier,
            MetadataObjectType.LooseDocuments,
            MetadataObjectType.AccessRequest,
        };

        public static readonly IReadOnlyList<MetadataObjectType> CapTaiLieu = new[]
        {
            MetadataObjectType.TextDocument,
            MetadataObjectType.Image,
            MetadataObjectType.Media,
        };

        public static bool LaCapGoi(MetadataObjectType objectType) => CapGoi.Contains(objectType);

        public static bool LaCapTaiLieu(MetadataObjectType objectType) => CapTaiLieu.Contains(objectType);

        public static string Ghi(IEnumerable<MetadataObjectType> objectTypes) =>
            string.Join(KiemTraConstants.DauPhanCachLoai, objectTypes.Select(x => (int)x));

        public static List<MetadataObjectType> Doc(string? luuTru)
        {
            if (string.IsNullOrWhiteSpace(luuTru))
            {
                return new List<MetadataObjectType> { MetadataObjectType.TextDocument };
            }

            return luuTru
                .Split(KiemTraConstants.DauPhanCachLoai, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var so) ? (MetadataObjectType?)so : null)
                .Where(x => x != null)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();
        }
    }
}
