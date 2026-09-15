namespace kssm.be.shared.Constants.DongGoi
{
    public static class MetadataObjectTypeLabels
    {
        public static readonly IReadOnlyDictionary<MetadataObjectType, string> Values =
            new Dictionary<MetadataObjectType, string>
            {
                [MetadataObjectType.Dossier] = "Hồ sơ",
                [MetadataObjectType.LooseDocuments] = "Tài liệu rời lẻ",
                [MetadataObjectType.AccessRequest] = "Yêu cầu khai thác",
                [MetadataObjectType.TextDocument] = "Văn bản",
                [MetadataObjectType.Image] = "Phim âm bản, ảnh",
                [MetadataObjectType.Media] = "Ghi âm, ghi hình",
            };

        public static string Get(MetadataObjectType objectType) =>
            Values.TryGetValue(objectType, out var label) ? label : objectType.ToString();
    }

    public static class PackageTypeLabels
    {
        public static readonly IReadOnlyDictionary<PackageType, string> Values =
            new Dictionary<PackageType, string>
            {
                [PackageType.Sip] = ChuanDongGoiTT05.ChuanDongGoiSIP,
                [PackageType.Aip] = ChuanDongGoiTT05.ChuanDongGoiAIP,
                [PackageType.Dip] = ChuanDongGoiTT05.ChuanDongGoiDIP,
            };

        public static string Get(PackageType packageType) =>
            Values.TryGetValue(packageType, out var label) ? label : packageType.ToString();
    }
}
