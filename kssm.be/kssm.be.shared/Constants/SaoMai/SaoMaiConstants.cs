namespace kssm.be.shared.Constants.SaoMai
{
    /// <summary>
    /// Tên collection, tên trường và giá trị trạng thái của MongoDB bên sao_mai. Service này chỉ ĐỌC, nhưng
    /// đọc theo tên do bên kia đặt — gom vào một chỗ để đổi tên bên đó là sửa đúng một file.
    /// </summary>
    public static class SaoMaiConstants
    {
        // Mongoose đặt tên collection bằng cách viết thường rồi thêm "s" vào tên model.
        public const string CollectionProjects = "projects";
        public const string CollectionProjectConfigs = "projectocrconfigs";
        public const string CollectionDocuments = "ocrdocuments";

        public const string FieldId = "_id";
        public const string FieldDeleted = "isDelete";
        public const string FieldProjectName = "name";
        public const string FieldProjectCode = "code";
        public const string FieldProjectStatus = "status";

        public const string FieldConfigProject = "project_id";
        public const string FieldMinioEndpoint = "minioEndpoint";
        public const string FieldMinioUseSsl = "minioUseSSL";
        public const string FieldMinioAccessKey = "minioAccessKey";
        public const string FieldMinioSecretKey = "minioSecretKey";
        public const string FieldBucket = "storageBucket";
        public const string FieldStorageProvider = "fileStorageProvider";

        public const string FieldDocumentProject = "project_id";
        public const string FieldPdfUrl = "pdfUrl";
        public const string FieldOriginalFileName = "originalFileName";
        public const string FieldPageCount = "pageCount";
        public const string FieldOrderIndex = "orderIndex";
        public const string FieldCreatedAt = "createdAt";

        /// <summary>Chỉ dự án ĐANG NGHIỆM THU mới được đưa vào ký số.</summary>
        public const string StatusInAcceptance = "dang_nghiem_thu";

        /// <summary>
        /// Trạng thái đặt cho dự án sau khi lô ký chạy trọn. Giá trị này CHƯA có trong enum PROJECT_STATUS
        /// bên sao_mai_be — phải thêm ở đó trước, nếu không lời gọi đổi trạng thái bị đánh trượt.
        /// </summary>
        public const string StatusSigned = "da_ky_so";

        /// <summary>Giá trị fileStorageProvider mà luồng ký làm việc được; dự án dùng giá trị khác thì trượt sớm.</summary>
        public const string StorageProviderMinio = "minio";

    }
}
