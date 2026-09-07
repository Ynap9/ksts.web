using kssm.be.external.Mongo.Dtos;
using kssm.be.external.Mongo.Interfaces;
using kssm.be.external.S3.Dtos;
using kssm.be.shared.Constants.SaoMai;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using kssm.be.shared.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace kssm.be.external.Mongo.Implements
{
    public class ProjectReader : IProjectReader
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<ProjectReader> _logger;

        public ProjectReader(IOptions<SaoMaiSettings> options, ILogger<ProjectReader> logger)
        {
            _logger = logger;

            var connectionString = options.Value.MongoConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy connection string \"MongoDb\" trong appsettings.json");
            }

            var url = MongoUrl.Create(connectionString);
            _database = new MongoClient(url).GetDatabase(url.DatabaseName);
        }

        public async Task<IReadOnlyList<ProjectDto>> GetSignableProjectsAsync(
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(SaoMaiConstants.FieldProjectStatus,
                    SaoMaiConstants.StatusInAcceptance)
                & NotDeleted();

            var result = await _database.GetCollection<BsonDocument>(SaoMaiConstants.CollectionProjects)
                .Find(filter)
                .SortBy(x => x[SaoMaiConstants.FieldProjectName])
                .ToListAsync(cancellationToken);

            return result.Select(ToProject).ToList();
        }

        public async Task<ProjectDto> GetProjectAsync(string projectId,
            CancellationToken cancellationToken = default)
        {
            var doc = await _database.GetCollection<BsonDocument>(SaoMaiConstants.CollectionProjects)
                .Find(Builders<BsonDocument>.Filter.Eq(SaoMaiConstants.FieldId, ToObjectId(projectId)) & NotDeleted())
                .FirstOrDefaultAsync(cancellationToken);

            if (doc == null)
            {
                throw new UserFriendlyException(ErrorCodes.DuAnNotFound, $"Không tìm thấy dự án {projectId}.");
            }

            return ToProject(doc);
        }

        public async Task<S3ConnectionDto?> GetProjectStorageAsync(string projectId,
            CancellationToken cancellationToken = default)
        {
            var doc = await _database.GetCollection<BsonDocument>(SaoMaiConstants.CollectionProjectConfigs)
                .Find(Builders<BsonDocument>.Filter.Eq(SaoMaiConstants.FieldConfigProject,
                    ToObjectId(projectId)) & NotDeleted())
                .FirstOrDefaultAsync(cancellationToken);

            if (doc == null)
            {
                throw new UserFriendlyException(ErrorCodes.DuAnChuaCauHinhMinio,
                    "Dự án chưa có cấu hình lưu trữ file.");
            }

            var provider = ReadString(doc, SaoMaiConstants.FieldStorageProvider);
            if (!string.Equals(provider, SaoMaiConstants.StorageProviderMinio,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException(ErrorCodes.DuAnKhongDungMinio,
                    $"Dự án đang lưu file ở \"{provider}\", luồng ký số chỉ làm việc với MinIO.");
            }

            var endpoint = ReadString(doc, SaoMaiConstants.FieldMinioEndpoint);
            var bucket = ReadString(doc, SaoMaiConstants.FieldBucket);
            var accessKey = ReadString(doc, SaoMaiConstants.FieldMinioAccessKey);
            var secretKey = ReadString(doc, SaoMaiConstants.FieldMinioSecretKey);

            // Thiếu một mảnh nào đó thì sao_mai_be đã lùi về MinIO của biến môi trường lúc nhận file, nên
            // bản nguồn nằm ở kho mặc định chứ không phải kho riêng của dự án.
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(bucket)
                || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            {
                return null;
            }

            // Endpoint bên sao_mai lưu dạng "host[:port]" và có thể kèm cả scheme; cờ SSL mới là thứ quyết
            // định giao thức, nên dựng lại URL từ hai mảnh đó thay vì tin phần scheme trong chuỗi.
            var useSsl = !doc.Contains(SaoMaiConstants.FieldMinioUseSsl)
                || doc[SaoMaiConstants.FieldMinioUseSsl].ToBoolean();
            var host = endpoint.Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .TrimEnd('/');

            return new S3ConnectionDto
            {
                Url = $"{(useSsl ? "https" : "http")}://{host}",
                Bucket = bucket,
                AccessKey = accessKey,
                SecretKey = secretKey,
                WithSSL = useSsl,
            };
        }

        public async Task<IReadOnlyList<ProjectFileDto>> GetPdfFilesAsync(string projectId, S3ConnectionDto storage,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(SaoMaiConstants.FieldDocumentProject, ToObjectId(projectId))
                & Builders<BsonDocument>.Filter.Ne(SaoMaiConstants.FieldPdfUrl, BsonNull.Value)
                & Builders<BsonDocument>.Filter.Ne(SaoMaiConstants.FieldPdfUrl, string.Empty)
                & NotDeleted();

            var docs = await _database.GetCollection<BsonDocument>(SaoMaiConstants.CollectionDocuments)
                .Find(filter)
                .Sort(Builders<BsonDocument>.Sort
                    .Ascending(SaoMaiConstants.FieldOrderIndex)
                    .Ascending(SaoMaiConstants.FieldCreatedAt))
                .ToListAsync(cancellationToken);

            var result = new List<ProjectFileDto>();

            foreach (var doc in docs)
            {
                var objectKey = ExtractObjectKey(ReadString(doc, SaoMaiConstants.FieldPdfUrl), storage);
                if (objectKey == null)
                {
                    // File nằm ở kho khác kho đang khai của dự án (dữ liệu cũ, hoặc đổi bucket giữa chừng).
                    // Bỏ qua chứ không đánh trượt cả lô: đường dẫn đó ký số không với tới được.
                    _logger.LogWarning("Bỏ qua tài liệu {Id}: pdfUrl không thuộc kho của dự án",
                        doc[SaoMaiConstants.FieldId]);
                    continue;
                }

                result.Add(new ProjectFileDto
                {
                    Id = doc[SaoMaiConstants.FieldId].ToString() ?? string.Empty,
                    FileName = ReadFileName(doc, objectKey),
                    ObjectKey = objectKey,
                    PageCount = doc.Contains(SaoMaiConstants.FieldPageCount)
                        && doc[SaoMaiConstants.FieldPageCount].IsInt32
                            ? doc[SaoMaiConstants.FieldPageCount].AsInt32
                            : null,
                });
            }

            return result;
        }

        /// <summary>
        /// Cắt URL canonical của sao_mai về object key. Khớp đúng tiền tố "{url}/{bucket}/" trước; không
        /// khớp thì tìm đoạn "/{bucket}/" trong đường dẫn, vì cùng một kho có thể được khai bằng tên miền
        /// khác nhau giữa hai hệ thống.
        /// </summary>
        public string? ExtractObjectKey(string pdfUrl, S3ConnectionDto storage)
        {
            if (string.IsNullOrWhiteSpace(pdfUrl))
            {
                return null;
            }

            var prefix = $"{storage.Url.TrimEnd('/')}/{storage.Bucket}/";
            if (pdfUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pdfUrl[prefix.Length..]);
            }

            var marker = $"/{storage.Bucket}/";
            var index = pdfUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

            return index < 0 ? null : Uri.UnescapeDataString(pdfUrl[(index + marker.Length)..]);
        }

        public string ReadFileName(BsonDocument doc, string objectKey)
        {
            var originalName = ReadString(doc, SaoMaiConstants.FieldOriginalFileName);

            // Tên file gốc do người dùng đặt nên có thể trùng nhau giữa hai tài liệu; tên lấy từ object key
            // là tên đang có thật trên kho, dùng làm phương án lùi.
            return string.IsNullOrWhiteSpace(originalName)
                ? objectKey[(objectKey.LastIndexOf('/') + 1)..]
                : originalName;
        }

        public ProjectDto ToProject(BsonDocument doc) => new()
        {
            Id = doc[SaoMaiConstants.FieldId].ToString() ?? string.Empty,
            Name = ReadString(doc, SaoMaiConstants.FieldProjectName),
            Code = ReadString(doc, SaoMaiConstants.FieldProjectCode),
            Status = ReadString(doc, SaoMaiConstants.FieldProjectStatus),
        };

        public string ReadString(BsonDocument doc, string field) =>
            doc.Contains(field) && !doc[field].IsBsonNull ? doc[field].AsString : string.Empty;

        /// <summary>Bản ghi đã xoá mềm mang cờ isDelete; bản ghi cũ có thể thiếu hẳn trường đó.</summary>
        public FilterDefinition<BsonDocument> NotDeleted() =>
            Builders<BsonDocument>.Filter.Ne(SaoMaiConstants.FieldDeleted, true);

        public ObjectId ToObjectId(string value) => ObjectId.TryParse(value, out var id)
            ? id
            : throw new UserFriendlyException(ErrorCodes.DuAnNotFound, $"Id dự án \"{value}\" không hợp lệ.");
    }
}
