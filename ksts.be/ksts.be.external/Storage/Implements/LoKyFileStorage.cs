using ksts.be.external.Storage.Interfaces;
using ksts.be.shared.Constants.LoKy;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace ksts.be.external.Storage.Implements
{
    public class LoKyFileStorage : ILoKyFileStorage
    {
        private readonly IS3FileStorage _s3FileStorage;

        public LoKyFileStorage(IS3FileStorage s3FileStorage)
        {
            _s3FileStorage = s3FileStorage;
        }

        public async Task<string> LuuFileNguonAsync(int loKyId, int thuTu, IFormFile file,
            CancellationToken cancellationToken = default)
        {
            var objectKey = BuildObjectKey(loKyId, LoKyConstants.ThuMucNguon, thuTu);
            await _s3FileStorage.UploadAsync(file, objectKey, cancellationToken);
            return objectKey;
        }

        public Task<byte[]> TaiAsync(string objectKey, CancellationToken cancellationToken = default) =>
            _s3FileStorage.DownloadAsync(objectKey, cancellationToken);

        public Task XoaLoAsync(int loKyId, CancellationToken cancellationToken = default) =>
            _s3FileStorage.DeleteByPrefixAsync(
                $"{LoKyConstants.ObjectKeyPrefix}/{loKyId}/", cancellationToken);

        public Task XoaThuMucAsync(int loKyId, string thuMuc, CancellationToken cancellationToken = default) =>
            _s3FileStorage.DeleteByPrefixAsync(
                $"{LoKyConstants.ObjectKeyPrefix}/{loKyId}/{thuMuc}/", cancellationToken);

        public string BuildObjectKey(int loKyId, string thuMuc, int thuTu) =>
            string.Format(CultureInfo.InvariantCulture, LoKyConstants.DinhDangObjectKey,
                LoKyConstants.ObjectKeyPrefix, loKyId, thuMuc, thuTu);
    }
}
