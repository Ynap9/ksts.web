using Amazon.S3;
using Amazon.S3.Model;
using ksts.be.external.Storage.Dtos;
using ksts.be.external.Storage.Interfaces;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using ksts.be.shared.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ksts.be.external.Storage.Implements
{
    /// <summary>
    /// Kho object trên MinIO qua AWS SDK.
    ///
    /// Cấu hình bắt buộc ForcePathStyle và AuthenticationRegion: MinIO KHÔNG hỗ trợ virtual-host style
    /// (bucket.endpoint), còn RegionEndpoint sẽ ép SDK đi tới địa chỉ AWS thật thay vì endpoint tự dựng.
    /// Đăng ký Singleton vì AmazonS3Client giữ pool kết nối - tạo lại mỗi request là phí kết nối.
    ///
    /// Upload tắt UseChunkEncoding và checksum mặc định: SDK gửi thân request dạng aws-chunked kèm trailer,
    /// proxy đứng giữa (Cloudflare Tunnel) đóng khung lại luồng nên MinIO tính ra hash khác header
    /// x-amz-content-sha256 và từ chối. Tắt đi thì hash tính một lần trên toàn bộ nội dung.
    /// </summary>
    public class S3FileStorage : IS3FileStorage
    {
        private readonly S3Settings _settings;
        private readonly AmazonS3Client _client;
        private readonly ILogger<S3FileStorage> _logger;

        public S3FileStorage(IOptions<S3Settings> options, ILogger<S3FileStorage> logger)
        {
            _settings = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.Bucket))
            {
                throw new UserFriendlyException(ErrorCodes.StorageNotConfigured,
                    "Thiếu cấu hình S3: cần S3_URL và S3_BUCKET trong appsettings.");
            }

            var withSSL = _settings.WithSSL;

            _client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, new AmazonS3Config
            {
                ServiceURL = _settings.Url,
                ForcePathStyle = true,
                AuthenticationRegion = _settings.Region,
                UseHttp = !withSSL,
            });
        }

        /// <inheritdoc/>
        public async Task<S3UploadResultDto> UploadAsync(IFormFile file, string objectKey,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Upload object {Key} ({Length} byte)", objectKey, file.Length);

            try
            {
                await using var stream = file.OpenReadStream();
                await _client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _settings.Bucket,
                    Key = objectKey,
                    InputStream = stream,
                    ContentType = file.ContentType,
                    CannedACL = S3CannedACL.PublicRead,
                    UseChunkEncoding = false,
                    DisableDefaultChecksumValidation = true,
                }, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Upload object {Key} thất bại", objectKey);
                throw new UserFriendlyException(ErrorCodes.StorageUploadFailed,
                    $"Không tải được ảnh lên kho lưu trữ: {ex.Message}");
            }

            return new S3UploadResultDto
            {
                ObjectKey = objectKey,
                Url = BuildPublicUrl(objectKey),
                FileName = file.FileName,
                Length = file.Length,
            };
        }

        /// <inheritdoc/>
        public async Task<S3UploadResultDto> UploadBytesAsync(byte[] noiDung, string objectKey,
            string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                using var stream = new MemoryStream(noiDung);
                await _client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _settings.Bucket,
                    Key = objectKey,
                    InputStream = stream,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.PublicRead,
                    UseChunkEncoding = false,
                    DisableDefaultChecksumValidation = true,
                }, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Upload object {Key} thất bại", objectKey);
                throw new UserFriendlyException(ErrorCodes.StorageUploadFailed,
                    $"Không tải được file lên kho lưu trữ: {ex.Message}");
            }

            return new S3UploadResultDto
            {
                ObjectKey = objectKey,
                Url = BuildPublicUrl(objectKey),
                FileName = objectKey,
                Length = noiDung.Length,
            };
        }

        /// <inheritdoc/>
        public async Task<byte[]> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = _settings.Bucket,
                    Key = objectKey,
                }, cancellationToken);

                using var output = new MemoryStream();
                await response.ResponseStream.CopyToAsync(output, cancellationToken);
                return output.ToArray();
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Tải object {Key} thất bại", objectKey);
                throw new UserFriendlyException(ErrorCodes.StorageDownloadFailed,
                    $"Không tải được file từ kho lưu trữ: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
            {
                return false;
            }

            try
            {
                await _client.GetObjectMetadataAsync(_settings.Bucket, objectKey, cancellationToken);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<string>> ListKeysAsync(string keyPrefix,
            CancellationToken cancellationToken = default)
        {
            var ketQua = new List<string>();
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                return ketQua;
            }

            try
            {
                string? token = null;
                do
                {
                    var listed = await _client.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = _settings.Bucket,
                        Prefix = keyPrefix,
                        ContinuationToken = token,
                    }, cancellationToken);

                    ketQua.AddRange((listed.S3Objects ?? new List<S3Object>()).Select(x => x.Key));

                    // Chỉ đi tiếp khi S3 báo còn trang sau; bám theo token của chính nó thay vì tự đoán.
                    token = listed.IsTruncated == true ? listed.NextContinuationToken : null;
                }
                while (!string.IsNullOrEmpty(token));
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Liệt kê object theo tiền tố {Prefix} thất bại", keyPrefix);
                throw new UserFriendlyException(ErrorCodes.StorageDownloadFailed,
                    $"Không đọc được danh sách file trong kho: {ex.Message}");
            }

            return ketQua;
        }

        /// <inheritdoc/>
        public async Task CopyAsync(string objectKeyNguon, string objectKeyDich,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _client.CopyObjectAsync(new CopyObjectRequest
                {
                    SourceBucket = _settings.Bucket,
                    SourceKey = objectKeyNguon,
                    DestinationBucket = _settings.Bucket,
                    DestinationKey = objectKeyDich,
                    CannedACL = S3CannedACL.PublicRead,
                }, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Chép object {Nguon} sang {Dich} thất bại", objectKeyNguon, objectKeyDich);
                throw new UserFriendlyException(ErrorCodes.StorageUploadFailed,
                    $"Không chép được file trong kho lưu trữ: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
            {
                return;
            }

            try
            {
                await _client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = _settings.Bucket,
                    Key = objectKey,
                }, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogWarning(ex, "Xoá object {Key} thất bại", objectKey);
            }
        }

        /// <inheritdoc/>
        public async Task DeleteByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                return;
            }

            try
            {
                // Đi qua ListKeysAsync để lấy ĐỦ mọi trang: liệt kê một lượt chỉ ra 1000 key đầu, xoá lô
                // 5000 file kiểu đó là bỏ sót bốn phần năm rồi báo đã dọn xong.
                foreach (var key in await ListKeysAsync(keyPrefix, cancellationToken))
                {
                    await _client.DeleteObjectAsync(new DeleteObjectRequest
                    {
                        BucketName = _settings.Bucket,
                        Key = key,
                    }, cancellationToken);
                }
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogWarning(ex, "Xoá object theo tiền tố {Prefix} thất bại", keyPrefix);
            }
        }

        /// <inheritdoc/>
        public string BuildPublicUrl(string objectKey) =>
            $"{_settings.Url.TrimEnd('/')}/{_settings.Bucket}/{objectKey}";
    }
}
