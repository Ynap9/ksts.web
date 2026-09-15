
using kssm.be.external.DongGoi.Interfaces;
using kssm.be.external.Drive.Interfaces;
using kssm.be.external.S3.Interfaces;
using kssm.be.shared.Constants.Drive;
using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Settings;
using Microsoft.Extensions.Options;

namespace kssm.be.external.DongGoi.Implements
{
    public class PackageFileStorage : IPackageFileStorage
    {
        private readonly IS3FileStorage _storage;
        private readonly IDriveFileStorage _driveFileStorage;
        private readonly DriveSettings _driveSettings;

        public PackageFileStorage(IS3FileStorage storage, IDriveFileStorage driveFileStorage,
            IOptions<DriveSettings> driveSettings)
        {
            _storage = storage;
            _driveFileStorage = driveFileStorage;
            _driveSettings = driveSettings.Value;
        }

        public async Task<string> SaveSourceAsync(byte[] content, int sessionId, int sequence, string extension,
            string contentType, CancellationToken cancellationToken = default)
        {
            var objectKey = KiemTraConstants.GetSourceObjectKey(sessionId, sequence, extension);
            await _storage.UploadBytesAsync(content, objectKey, contentType, cancellationToken);
            return objectKey;
        }

        public async Task<string> EnsurePackageFolderAsync(string tenThuMuc,
            CancellationToken cancellationToken = default)
        {
            var thuMuc = await _driveFileStorage.EnsureFolderAsync(_driveSettings.PackageFolderId, tenThuMuc,
                cancellationToken);

            return thuMuc.Id;
        }

        public Task<string> SavePackageAsync(string driveFolderId, byte[] content, string tenFile,
            CancellationToken cancellationToken = default)
        {
            return _driveFileStorage.UploadAsync(driveFolderId, content, tenFile,
                SipPackConstants.MimeTypeZip, cancellationToken);
        }

        public Task<byte[]> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            return _storage.DownloadAsync(objectKey, cancellationToken);
        }

        public string BuildPackageUrl(string driveFileId)
        {
            return DriveConstants.GetFileUrl(driveFileId);
        }

        public Task RemoveSessionAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            return _storage.DeleteByPrefixAsync(KiemTraConstants.GetSessionPrefix(sessionId), cancellationToken);
        }
    }
}
