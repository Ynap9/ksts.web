
using kssm.be.external.DongGoi.Interfaces;
using kssm.be.external.S3.Interfaces;
using kssm.be.shared.Constants.DongGoi;

namespace kssm.be.external.DongGoi.Implements
{
    public class PackageFileStorage : IPackageFileStorage
    {
        private readonly IS3FileStorage _storage;

        public PackageFileStorage(IS3FileStorage storage)
        {
            _storage = storage;
        }

        public async Task<string> SaveSourceAsync(byte[] content, int sessionId, int sequence, string extension,
            string contentType, CancellationToken cancellationToken = default)
        {
            var objectKey = KiemTraConstants.GetSourceObjectKey(sessionId, sequence, extension);
            await _storage.UploadBytesAsync(content, objectKey, contentType, cancellationToken);
            return objectKey;
        }

        public async Task<string> SavePackageAsync(byte[] content, int sessionId, string tenFile,
            CancellationToken cancellationToken = default)
        {
            var objectKey = KiemTraConstants.GetPackageObjectKey(sessionId, tenFile);
            await _storage.UploadBytesAsync(content, objectKey, SipPackConstants.MimeTypeZip, cancellationToken);
            return objectKey;
        }

        public Task<byte[]> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            return _storage.DownloadAsync(objectKey, cancellationToken);
        }

        public string BuildPublicUrl(string objectKey)
        {
            return _storage.BuildPublicUrl(objectKey);
        }

        public Task RemoveSessionAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            return _storage.DeleteByPrefixAsync(KiemTraConstants.GetSessionPrefix(sessionId), cancellationToken);
        }
    }
}
