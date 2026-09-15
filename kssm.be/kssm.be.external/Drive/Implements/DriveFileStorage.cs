using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using kssm.be.external.Drive.Dtos;
using kssm.be.external.Drive.Interfaces;
using kssm.be.shared.Constants.Drive;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using kssm.be.shared.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace kssm.be.external.Drive.Implements
{
    public class DriveFileStorage : IDriveFileStorage
    {
        private readonly DriveSettings _settings;
        private readonly ILogger<DriveFileStorage> _logger;
        private readonly Lazy<DriveService> _service;
        private readonly SemaphoreSlim _khoaTaoThuMuc = new(1, 1);

        public DriveFileStorage(IOptions<DriveSettings> settings, ILogger<DriveFileStorage> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _service = new Lazy<DriveService>(DungDichVu, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public DriveService DungDichVu()
        {
            var duongDanKhoa = LayDuongDanKhoa();
            if (!File.Exists(duongDanKhoa))
            {
                throw new UserFriendlyException(ErrorCodes.DriveNotConfigured,
                    $"Không tìm thấy khoá service account của Google Drive: {duongDanKhoa}");
            }

            GoogleCredential credential;
            using (var stream = new FileStream(duongDanKhoa, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(DriveService.Scope.Drive);
            }

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = DriveConstants.ApplicationName,
            });
        }

        public string LayDuongDanKhoa()
        {
            if (string.IsNullOrWhiteSpace(_settings.CredentialsPath))
            {
                throw new UserFriendlyException(ErrorCodes.DriveNotConfigured,
                    "Chưa khai đường dẫn khoá service account của Google Drive (Drive:DRIVE_CREDENTIALS_PATH).");
            }

            return Path.IsPathRooted(_settings.CredentialsPath)
                ? _settings.CredentialsPath
                : Path.Combine(AppContext.BaseDirectory, _settings.CredentialsPath);
        }

        public async Task<DriveFolderDto> EnsureFolderAsync(string rootFolderId, string tenThuMuc,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rootFolderId))
            {
                throw new UserFriendlyException(ErrorCodes.DriveNotConfigured,
                    "Chưa khai thư mục gốc tương ứng trên Google Drive ở mục cấu hình Drive.");
            }

            if (string.IsNullOrWhiteSpace(tenThuMuc))
            {
                throw new UserFriendlyException(ErrorCodes.DriveFolderFailed,
                    "Chưa có tên thư mục đích trên Google Drive.");
            }

            await _khoaTaoThuMuc.WaitAsync(cancellationToken);
            try
            {
                var id = await TimThuMucAsync(rootFolderId, tenThuMuc, cancellationToken)
                    ?? await TaoThuMucAsync(rootFolderId, tenThuMuc, cancellationToken);

                return new DriveFolderDto { Id = id, Url = DriveConstants.GetFolderUrl(id) };
            }
            finally
            {
                _khoaTaoThuMuc.Release();
            }
        }

        public async Task<string?> TimThuMucAsync(string rootFolderId, string tenThuMuc,
            CancellationToken cancellationToken)
        {
            var request = _service.Value.Files.List();
            request.Q = string.Format(DriveConstants.FolderQueryFormat, DriveConstants.FolderMimeType,
                DriveConstants.EscapeQueryValue(tenThuMuc), rootFolderId);
            request.Fields = DriveConstants.FolderQueryFields;
            request.SupportsAllDrives = true;
            request.IncludeItemsFromAllDrives = true;
            request.PageSize = 1;

            try
            {
                var ketQua = await request.ExecuteAsync(cancellationToken);
                return ketQua.Files?.FirstOrDefault()?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tìm thư mục {TenThuMuc} trên Google Drive thất bại", tenThuMuc);
                throw new UserFriendlyException(ErrorCodes.DriveFolderFailed,
                    $"Không đọc được thư mục trên Google Drive: {ex.Message}");
            }
        }

        public async Task<string> TaoThuMucAsync(string rootFolderId, string tenThuMuc,
            CancellationToken cancellationToken)
        {
            var request = _service.Value.Files.Create(new DriveFile
            {
                Name = tenThuMuc,
                MimeType = DriveConstants.FolderMimeType,
                Parents = new List<string> { rootFolderId },
            });
            request.Fields = DriveConstants.UploadFields;
            request.SupportsAllDrives = true;

            try
            {
                var thuMuc = await request.ExecuteAsync(cancellationToken);
                _logger.LogInformation("Tạo thư mục {TenThuMuc} trên Google Drive: {Id}", tenThuMuc, thuMuc.Id);

                return thuMuc.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tạo thư mục {TenThuMuc} trên Google Drive thất bại", tenThuMuc);
                throw new UserFriendlyException(ErrorCodes.DriveFolderFailed,
                    $"Không tạo được thư mục \"{tenThuMuc}\" trên Google Drive: {ex.Message}");
            }
        }

        public async Task<string> UploadAsync(string folderId, byte[] content, string tenFile,
            string contentType, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream(content);

            var request = _service.Value.Files.Create(
                new DriveFile { Name = tenFile, Parents = new List<string> { folderId } },
                stream,
                contentType);
            request.Fields = DriveConstants.UploadFields;
            request.SupportsAllDrives = true;

            var tienTrinh = await request.UploadAsync(cancellationToken);
            if (tienTrinh.Status != UploadStatus.Completed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogError(tienTrinh.Exception, "Đẩy {TenFile} lên Google Drive thất bại", tenFile);
                throw new UserFriendlyException(ErrorCodes.DriveUploadFailed,
                    $"Không đẩy được file \"{tenFile}\" lên Google Drive: {tienTrinh.Exception?.Message}");
            }

            return request.ResponseBody?.Id ?? string.Empty;
        }
    }
}
