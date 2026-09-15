using kssm.be.shared.Constants.DongGoi;

namespace kssm.be.infrastructure.Persistence.Seeder
{
    public record CodeValueSeed(
        string CodeGroup,
        string Code,
        string DisplayName,
        int SortOrder);

    public record MetadataFieldSeed(
        string FieldKey,
        string DisplayName,
        MetadataDataType DataType,
        int? FieldLength,
        string? CodeGroup,
        string? Description);

    public record FieldConfigSeed(
        string FieldKey,
        FieldRequirement Requirement,
        string? EadElement = null,
        string? ConditionField = null,
        string? ConditionValues = null,
        int? FieldLengthOverride = null,
        MetadataDataType? DataTypeOverride = null,
        string? DescriptionOverride = null);

    public record PackageVariantSeed(
        string VariantKey,
        PackageType PackageType,
        MetadataObjectType ObjectType,
        IReadOnlyList<FieldConfigSeed> Fields);

    public static class DongGoiSeedData
    {
        public static readonly IReadOnlyList<CodeValueSeed> CodeValues = new CodeValueSeed[]
        {
            new("maintenance", "01", "Vĩnh viễn", 1),
            new("maintenance", "02", "70 năm", 2),
            new("maintenance", "03", "50 năm", 3),
            new("maintenance", "04", "30 năm", 4),
            new("maintenance", "05", "20 năm", 5),
            new("maintenance", "06", "10 năm", 6),
            new("maintenance", "07", "Khác", 7),

            new("mode", "01", "Công khai", 1),
            new("mode", "02", "Sử dụng có điều kiện", 2),
            new("mode", "03", "Mật", 3),

            new("language", "01", "Tiếng Việt", 1),
            new("language", "02", "Tiếng Anh", 2),
            new("language", "03", "Tiếng Pháp", 3),
            new("language", "04", "Tiếng Nga", 4),
            new("language", "05", "Tiếng Trung", 5),
            new("language", "06", "Việt Anh", 6),
            new("language", "07", "Việt Nga", 7),
            new("language", "08", "Việt Pháp", 8),
            new("language", "09", "Hán Nôm", 9),
            new("language", "10", "Việt Trung", 10),
            new("language", "11", "Khác", 11),

            new("confidenceLevel", "01", "Gốc điện tử", 1),
            new("confidenceLevel", "02", "Số hóa", 2),
            new("confidenceLevel", "03", "Hỗn hợp", 3),

            new("format", "01", "Tốt", 1),
            new("format", "02", "Bình thường", 2),
            new("format", "03", "Hỏng", 3),

            new("riskRecovery", "1", "Có", 1),
            new("riskRecovery", "0", "Không", 2),

            new("riskRecoveryStatus", "01", "Đã dự phòng", 1),
            new("riskRecoveryStatus", "02", "Chưa dự phòng", 2),

            new("typeName", "01", "Nghị quyết", 1),
            new("typeName", "02", "Quyết định", 2),
            new("typeName", "03", "Chỉ thị", 3),
            new("typeName", "04", "Quy chế", 4),
            new("typeName", "05", "Quy định", 5),
            new("typeName", "06", "Thông cáo", 6),
            new("typeName", "07", "Thông báo", 7),
            new("typeName", "08", "Hướng dẫn", 8),
            new("typeName", "09", "Chương trình", 9),
            new("typeName", "10", "Kế hoạch", 10),
            new("typeName", "11", "Phương án", 11),
            new("typeName", "12", "Đề án", 12),
            new("typeName", "13", "Dự án", 13),
            new("typeName", "14", "Báo cáo", 14),
            new("typeName", "15", "Tờ trình", 15),
            new("typeName", "16", "Giấy ủy quyền", 16),
            new("typeName", "17", "Phiếu gửi", 17),
            new("typeName", "18", "Phiếu chuyển", 18),
            new("typeName", "19", "Phiếu báo", 19),
            new("typeName", "20", "Biên bản", 20),
            new("typeName", "21", "Hợp đồng", 21),
            new("typeName", "22", "Công văn", 22),
            new("typeName", "23", "Công điện", 23),
            new("typeName", "24", "Bản ghi nhớ", 24),
            new("typeName", "25", "Bản thỏa thuận", 25),
            new("typeName", "26", "Giấy mời", 26),
            new("typeName", "27", "Giấy giới thiệu", 27),
            new("typeName", "28", "Giấy nghỉ phép", 28),
            new("typeName", "29", "Thư công", 29),
            new("typeName", "30", "Bản đồ", 30),
            new("typeName", "31", "Bản vẽ kỹ thuật", 31),
            new("typeName", "32", "Khác", 32),

            new("purpose", "01", "Cá nhân", 1),
            new("purpose", "02", "Công vụ", 2),
            new("purpose", "03", "Công vụ đặc biệt", 3),

            new("feeObjectType", "01", "Học sinh, sinh viên, học viên, nghiên cứu sinh", 1),
            new("feeObjectType", "02", "Thân nhân liệt sĩ", 2),
            new("feeObjectType", "03", "Thương binh, bệnh binh", 3),
            new("feeObjectType", "04", "Người hoạt động kháng chiến", 4),
            new("feeObjectType", "05", "Người có công giúp đỡ cách mạng", 5),
            new("feeObjectType", "06", "Người thờ cúng liệt sỹ", 6),
            new("feeObjectType", "07", "Người hưởng chế độ hưu trí", 7),
            new("feeObjectType", "08", "Người mất sức lao động, tai nạn lao động", 8),
            new("feeObjectType", "09", "Người bị mắc bệnh nghề nghiệp", 9),
            new("feeObjectType", "10", "Khác", 10),
        };

        public static readonly IReadOnlyList<MetadataFieldSeed> MetadataFields = new MetadataFieldSeed[]
        {
            new("fileCode", "Mã hồ sơ", MetadataDataType.Text, 100, null,
                "Mã định danh cơ quan, tổ chức, cá nhân + năm hình thành hồ sơ + số và ký hiệu hồ sơ"),
            new("title", "Tiêu đề hồ sơ", MetadataDataType.Text, 1000, null, null),
            new("maintenance", "Thời hạn bảo quản", MetadataDataType.Text, 100, "maintenance",
                "Nguồn nộp lưu và sưu tầm chỉ nhận 01 Vĩnh viễn"),
            new("mode", "Chế độ sử dụng", MetadataDataType.Text, 30, "mode", null),
            new("language", "Ngôn ngữ", MetadataDataType.Text, 100, "language", "Chọn một hoặc nhiều giá trị"),
            new("startDate", "Thời gian bắt đầu", MetadataDataType.Date, null, null, "Định dạng DD/MM/YYYY"),
            new("endDate", "Thời gian kết thúc", MetadataDataType.Date, null, null, "Định dạng DD/MM/YYYY"),
            new("keyword", "Từ khóa", MetadataDataType.Text, 100, null, null),
            new("totalDoc", "Tổng số tài liệu trong hồ sơ", MetadataDataType.Number, 10, null,
                "Văn bản, tài liệu kỹ thuật, phim âm bản, ảnh, ghi âm, ghi hình"),
            new("numberOfPaper", "Số lượng tờ", MetadataDataType.Number, 10, null,
                "Dành cho tài liệu giấy đã số hóa"),
            new("numberOfPage", "Số lượng trang", MetadataDataType.Number, 10, null, null),
            new("format", "Tình trạng vật lý", MetadataDataType.Text, 50, "format", null),
            new("inforSign", "Ký hiệu thông tin", MetadataDataType.Text, 30, null, null),
            new("confidenceLevel", "Mức độ tin cậy", MetadataDataType.Text, 40, "confidenceLevel", null),
            new("paperFileCode", "Mã hồ sơ gốc giấy", MetadataDataType.Text, 100, null,
                "Mã cơ quan lưu trữ . Số kho, giá, hộp . Số hồ sơ giấy"),
            new("riskRecovery", "Chế độ dự phòng", MetadataDataType.Boolean, 1, "riskRecovery", null),
            new("riskRecoveryStatus", "Tình trạng dự phòng", MetadataDataType.Text, 2, "riskRecoveryStatus", null),
            new("description", "Ghi chú", MetadataDataType.Text, 2000, null,
                "Tên người lập hồ sơ và nội dung cần làm rõ"),

            new("source", "Nguồn gốc", MetadataDataType.Boolean, 1, null, "0 văn bản đi, 1 văn bản đến"),

            new("docId", "Mã định danh tài liệu", MetadataDataType.Text, 25, null, null),
            new("docCode", "Mã lưu trữ của tài liệu", MetadataDataType.Text, 100, null,
                "Mã hồ sơ + số thứ tự tài liệu trong hồ sơ, số thứ tự 7 ký tự"),
            new("docOrdinal", "Số thứ tự tài liệu", MetadataDataType.Number, null, null,
                "Số thứ tự tài liệu trong hồ sơ"),
            new("typeName", "Tên loại tài liệu", MetadataDataType.Text, 10, "typeName", null),
            new("codeNumber", "Số của tài liệu", MetadataDataType.Text, 11, null, null),
            new("codeNotation", "Ký hiệu của tài liệu", MetadataDataType.Text, 30, null, null),
            new("issuedDate", "Ngày, tháng, năm tài liệu", MetadataDataType.Date, null, null, "Định dạng DD/MM/YYYY"),
            new("organName", "Tên cơ quan, tổ chức, cá nhân ban hành tài liệu", MetadataDataType.Text, 200, null, null),
            new("subject", "Trích yếu nội dung", MetadataDataType.Text, 500, null, null),
            new("autograph", "Bút tích", MetadataDataType.Text, 2000, null, null),
            new("process", "Đường dẫn tài liệu Quy trình xử lý", MetadataDataType.Boolean, 1, null,
                "1 = có tệp luồng xử lý đi kèm"),

            new("requestID", "Mã yêu cầu khai thác", MetadataDataType.Text, 100, null, null),
            new("requestDate", "Ngày yêu cầu", MetadataDataType.Date, null, null, "Định dạng DD/MM/YYYY"),
            new("purpose", "Mục đích", MetadataDataType.Text, 100, "purpose", null),
            new("purposeContent", "Nội dung mục đích", MetadataDataType.Text, 500, null, null),
            new("feeObjectType", "Đối tượng", MetadataDataType.Text, 100, "feeObjectType",
                "Căn cứ tính phí đọc và phí cấp bản sao"),
            new("researchTopic", "Chủ đề nghiên cứu", MetadataDataType.Text, 250, null, null),
        };

        public static IReadOnlyList<FieldConfigSeed> DossierFields(PackageType packageType)
        {
            var archival = packageType != PackageType.Sip;
            var dateNote = archival ? "Định dạng DD/MM/YYYY, MM/YYYY hoặc YYYY" : null;

            var head = new FieldConfigSeed[]
            {
                new("fileCode", FieldRequirement.Required,
                    EadElement: archival ? "arcFileCode" : "fileCode",
                    DescriptionOverride: archival
                        ? "Mã cơ quan lưu trữ + Mã hồ sơ. Mã hồ sơ = Mã định danh cơ quan, tổ chức, cá nhân hoặc Mã phông + năm hình thành hồ sơ + số và ký hiệu hồ sơ + Mục lục số"
                        : null),
                new("title", FieldRequirement.Required),
                new("maintenance", FieldRequirement.Required),
                new("mode", FieldRequirement.Required),
                new("language", FieldRequirement.Required),
                new("startDate", FieldRequirement.Required, DescriptionOverride: dateNote),
                new("endDate", FieldRequirement.Required, DescriptionOverride: dateNote),
                new("keyword", FieldRequirement.Optional),
                new("totalDoc", FieldRequirement.Required),
                new("numberOfPaper", FieldRequirement.Required),
                new("numberOfPage", FieldRequirement.Required),
                new("format", FieldRequirement.Optional),
                new("inforSign", FieldRequirement.Optional),
                new("confidenceLevel", FieldRequirement.Optional),
                new("paperFileCode", FieldRequirement.Conditional,
                    ConditionField: "confidenceLevel", ConditionValues: "02"),
            };

            var riskRecovery = new FieldConfigSeed("riskRecovery", FieldRequirement.Required);
            var riskRecoveryStatus = new FieldConfigSeed("riskRecoveryStatus", FieldRequirement.Conditional,
                ConditionField: "riskRecovery", ConditionValues: "1");
            var description = new FieldConfigSeed("description", FieldRequirement.Optional);

            var tail = archival
                ? new[] { description, riskRecovery, riskRecoveryStatus }
                : new[] { riskRecovery, riskRecoveryStatus, description };

            return head.Concat(tail).ToList();
        }

        public static IReadOnlyList<FieldConfigSeed> SipTextDocumentFields() => new FieldConfigSeed[]
        {
            new("docId", FieldRequirement.Required),
            new("docCode", FieldRequirement.Required),
            new("maintenance", FieldRequirement.Required),
            new("typeName", FieldRequirement.Required),
            new("codeNumber", FieldRequirement.Optional),
            new("codeNotation", FieldRequirement.Optional),
            new("issuedDate", FieldRequirement.Required),
            new("organName", FieldRequirement.Required),
            new("subject", FieldRequirement.Required),
            new("language", FieldRequirement.Required),
            new("numberOfPage", FieldRequirement.Required, FieldLengthOverride: 4),
            new("inforSign", FieldRequirement.Optional),
            new("keyword", FieldRequirement.Optional),
            new("mode", FieldRequirement.Required, FieldLengthOverride: 20),
            new("confidenceLevel", FieldRequirement.Optional, FieldLengthOverride: 30),
            new("autograph", FieldRequirement.Optional),
            new("format", FieldRequirement.Optional),
            new("process", FieldRequirement.Conditional,
                ConditionField: "confidenceLevel", ConditionValues: "01,03"),
            new("riskRecovery", FieldRequirement.Required),
            new("riskRecoveryStatus", FieldRequirement.Conditional,
                ConditionField: "riskRecovery", ConditionValues: "1"),
            new("description", FieldRequirement.Optional, FieldLengthOverride: 500),
        };

        public static IReadOnlyList<FieldConfigSeed> AipTextDocumentFields() => new FieldConfigSeed[]
        {
            new("fileCode", FieldRequirement.Required,
                EadElement: "arcFileCode",
                DescriptionOverride: "Mã cơ quan lưu trữ + Mã hồ sơ chứa tài liệu này"),
            new("mode", FieldRequirement.Required, FieldLengthOverride: 20),
            new("language", FieldRequirement.Required),
            new("keyword", FieldRequirement.Optional),
            new("numberOfPage", FieldRequirement.Required, FieldLengthOverride: 4),
            new("format", FieldRequirement.Optional),
            new("inforSign", FieldRequirement.Optional),
            new("confidenceLevel", FieldRequirement.Optional, FieldLengthOverride: 30),
            new("description", FieldRequirement.Optional, FieldLengthOverride: 500),
            new("docCode", FieldRequirement.Required),
            new("docOrdinal", FieldRequirement.Required),
            new("typeName", FieldRequirement.Required),
            new("codeNumber", FieldRequirement.Optional),
            new("codeNotation", FieldRequirement.Optional),
            new("issuedDate", FieldRequirement.Required),
            new("organName", FieldRequirement.Required),
            new("subject", FieldRequirement.Required),
            new("autograph", FieldRequirement.Optional),
            new("process", FieldRequirement.Conditional,
                ConditionField: "confidenceLevel", ConditionValues: "01,03"),
        };

        public static IReadOnlyList<FieldConfigSeed> DipTextDocumentFields() => new FieldConfigSeed[]
        {
            new("docId", FieldRequirement.Required),
            new("docCode", FieldRequirement.Required,
                EadElement: "arcDocCode",
                DescriptionOverride: "Mã cơ quan lưu trữ + Mã hồ sơ + số thứ tự tài liệu trong hồ sơ, số thứ tự 7 ký tự"),
            new("maintenance", FieldRequirement.Required),
            new("typeName", FieldRequirement.Required),
            new("codeNumber", FieldRequirement.Optional),
            new("codeNotation", FieldRequirement.Optional),
            new("issuedDate", FieldRequirement.Required),
            new("subject", FieldRequirement.Required),
            new("language", FieldRequirement.Required),
            new("numberOfPage", FieldRequirement.Required, FieldLengthOverride: 4),
            new("inforSign", FieldRequirement.Optional),
            new("keyword", FieldRequirement.Optional),
            new("mode", FieldRequirement.Required, FieldLengthOverride: 20),
            new("confidenceLevel", FieldRequirement.Optional, FieldLengthOverride: 30),
            new("autograph", FieldRequirement.Optional),
            new("format", FieldRequirement.Optional),
            new("process", FieldRequirement.Conditional,
                ConditionField: "confidenceLevel", ConditionValues: "01,03"),
            new("riskRecovery", FieldRequirement.Required),
            new("riskRecoveryStatus", FieldRequirement.Conditional,
                ConditionField: "riskRecovery", ConditionValues: "1"),
            new("description", FieldRequirement.Optional, FieldLengthOverride: 500),
        };

        public static readonly IReadOnlyList<PackageVariantSeed> PackageVariants = new PackageVariantSeed[]
        {
            new("SIP_hoso", PackageType.Sip, MetadataObjectType.Dossier,
                DossierFields(PackageType.Sip)),

            new("SIP_tailieu", PackageType.Sip, MetadataObjectType.LooseDocuments,
                new FieldConfigSeed[]
                {
                    new("fileCode", FieldRequirement.Required,
                        DescriptionOverride: "Mã gói tin SIP_tailieu = Mã định danh cơ quan + năm hình thành tài liệu + số lần nộp lưu 2 ký tự + số thứ tự tài liệu trong lần nộp 7 ký tự"),
                    new("title", FieldRequirement.Required,
                        DescriptionOverride: "Tóm tắt nội dung và thời gian tài liệu trong gói tin"),
                    new("source", FieldRequirement.Required),
                    new("totalDoc", FieldRequirement.Required,
                        DescriptionOverride: "Tổng số tài liệu trong gói tin"),
                    new("description", FieldRequirement.Optional),
                }),

            new("AIP_hoso", PackageType.Aip, MetadataObjectType.Dossier,
                DossierFields(PackageType.Aip)),

            new("AIP_tailieu", PackageType.Aip, MetadataObjectType.LooseDocuments,
                new FieldConfigSeed[]
                {
                    new("fileCode", FieldRequirement.Required,
                        EadElement: "arcFileCode",
                        DescriptionOverride: "Mã cơ quan lưu trữ + Mã gói tin. Mã gói tin = Mã định danh cơ quan + năm hình thành tài liệu + số thứ tự lần nộp lưu + số thứ tự gói tin trong lần nộp"),
                    new("title", FieldRequirement.Required),
                    new("source", FieldRequirement.Required,
                        FieldLengthOverride: 100, DataTypeOverride: MetadataDataType.Text),
                    new("description", FieldRequirement.Optional),
                }),

            new("DIP_goi", PackageType.Dip, MetadataObjectType.AccessRequest,
                new FieldConfigSeed[]
                {
                    new("requestID", FieldRequirement.Required),
                    new("requestDate", FieldRequirement.Required),
                    new("purpose", FieldRequirement.Required),
                    new("purposeContent", FieldRequirement.Required),
                    new("feeObjectType", FieldRequirement.Required),
                    new("researchTopic", FieldRequirement.Required),
                    new("description", FieldRequirement.Optional),
                }),

            new("SIP_doc", PackageType.Sip, MetadataObjectType.TextDocument,
                SipTextDocumentFields()),

            new("AIP_doc", PackageType.Aip, MetadataObjectType.TextDocument,
                AipTextDocumentFields()),

            new("DIP_doc", PackageType.Dip, MetadataObjectType.TextDocument,
                DipTextDocumentFields()),
        };
    }
}
