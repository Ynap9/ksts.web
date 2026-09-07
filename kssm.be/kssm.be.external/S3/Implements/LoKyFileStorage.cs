using Amazon.S3;
using Amazon.S3.Model;
using kssm.be.external.S3.Dtos;
using kssm.be.external.S3.Interfaces;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace kssm.be.external.S3.Implements
{
    public class LoKyFileStorage : ILoKyFileStorage
    {
        private const int MaxKeysPerDelete = 1000;

        private readonly IS3ClientFactory _clientFactory;
        private readonly ILogger<LoKyFileStorage> _logger;

        public LoKyFileStorage(IS3ClientFactory clientFactory, ILogger<LoKyFileStorage> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<long> SaveSourceAsync(S3ConnectionDto storage, IFormFile file, string objectKey,
            CancellationToken cancellationToken = default)
        {
            await using var stream = file.OpenReadStream();

            try
            {
                await _clientFactory.Get(storage).PutObjectAsync(new PutObjectRequest
                {
                    BucketName = storage.Bucket,
                    Key = objectKey,
                    InputStream = stream,
                    ContentType = file.ContentType,
                    UseChunkEncoding = false,
                    DisableDefaultChecksumValidation = true,
                }, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Lưu file nguồn {Key} thất bại", objectKey);
                throw new UserFriendlyException(ErrorCodes.StorageUploadFailed,
                    $"Không tải được file \"{file.FileName}\" lên kho lưu trữ: {ex.Message}");
            }

            return file.Length;
        }

        public async Task UploadAsync(S3ConnectionDto storage, byte[] content, string objectKey,
            string contentType, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream(content);

            try
            {
                await _clientFactory.Get(storage).PutObjectAsync(new PutObjectRequest
                {
                    BucketName = storage.Bucket,
                    Key = objectKey,
                    InputStream = stream,
                    ContentType = contentType,
                    UseChunkEncoding = false,
                    DisableDefaultChecksumValidation = true,
                }, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Đẩy object {Key} lên kho thất bại", objectKey);
                throw new UserFriendlyException(ErrorCodes.StorageUploadFailed,
                    $"Không đẩy được file lên kho lưu trữ: {ex.Message}");
            }
        }

        public async Task<byte[]> DownloadAsync(S3ConnectionDto storage, string objectKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _clientFactory.Get(storage)
                    .GetObjectAsync(storage.Bucket, objectKey, cancellationToken);
                using var buffer = new MemoryStream();
                await response.ResponseStream.CopyToAsync(buffer, cancellationToken);

                return buffer.ToArray();
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Tải object {Key} từ kho thất bại", objectKey);
                throw new UserFriendlyException(ErrorCodes.StorageDownloadFailed,
                    $"Không tải được file \"{objectKey}\" từ kho lưu trữ: {ex.Message}");
            }
        }

        public async Task<IReadOnlyList<string>> ListKeysAsync(S3ConnectionDto storage, string prefix,
            CancellationToken cancellationToken = default)
        {
            var client = _clientFactory.Get(storage);
            var keys = new List<string>();
            string? continuationToken = null;

            try
            {
                do
                {
                    var response = await client.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = storage.Bucket,
                        Prefix = prefix,
                        ContinuationToken = continuationToken,
                    }, cancellationToken);

                    keys.AddRange(response.S3Objects.Select(x => x.Key));
                    continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
                }
                while (continuationToken != null);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Liệt kê object dưới {Prefix} thất bại", prefix);
                throw new UserFriendlyException(ErrorCodes.StorageDownloadFailed,
                    $"Không đọc được thư mục \"{prefix}\" trên kho lưu trữ: {ex.Message}");
            }

            return keys;
        }

        public async Task DeleteByPrefixAsync(S3ConnectionDto storage, string prefix,
            CancellationToken cancellationToken = default)
        {
            var keys = await ListKeysAsync(storage, prefix, cancellationToken);
            if (keys.Count == 0)
            {
                return;
            }

            var client = _clientFactory.Get(storage);

            // Xoá theo lô 1000 key — trần một lần gọi DeleteObjects của giao thức S3.
            foreach (var batch in keys.Chunk(MaxKeysPerDelete))
            {
                try
                {
                    await client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = storage.Bucket,
                        Objects = batch.Select(x => new KeyVersion { Key = x }).ToList(),
                    }, cancellationToken);
                }
                catch (AmazonS3Exception ex)
                {
                    _logger.LogError(ex, "Xoá object dưới {Prefix} thất bại", prefix);
                    throw new UserFriendlyException(ErrorCodes.StorageDeleteFailed,
                        $"Không xoá được file trên kho lưu trữ: {ex.Message}");
                }
            }
        }
    }
}
