using kssm.be.external.S3.Dtos;
using kssm.be.external.S3.Interfaces;
using kssm.be.shared.Constants.Template;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kssm.be.external.S3.Implements
{
    public class TemplateImageStorage : ITemplateImageStorage
    {
        private readonly IS3FileStorage _storage;

        public TemplateImageStorage(IS3FileStorage storage)
        {
            _storage = storage;
        }

        public async Task<S3UploadResultDto> SaveAsync(IFormFile file, int templateId, string objectName,
            string? oldObjectKey, CancellationToken cancellationToken = default)
        {
            ValidateImage(file);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var objectKey = TemplateConstants.GetAssetKey(templateId, objectName, extension);

            if (!string.IsNullOrWhiteSpace(oldObjectKey)
                && !string.Equals(oldObjectKey, objectKey, StringComparison.Ordinal))
            {
                await _storage.DeleteAsync(oldObjectKey, cancellationToken);
            }

            // Hỏi kho xem key này đã có gì chưa rồi mới ghi: có thì xoá hẳn bản cũ trước, chưa có thì ghi
            // thẳng. Ghi đè trần tuy cũng ra kết quả đúng nhưng để lại bản cũ trong lịch sử phiên bản của
            // kho và trong bộ đệm của tầng phát tán, nên người dùng tải lại ảnh xong vẫn thấy ảnh cũ.
            if (await _storage.ExistsAsync(objectKey, cancellationToken))
            {
                await _storage.DeleteAsync(objectKey, cancellationToken);
            }

            return await _storage.UploadAsync(file, objectKey, cancellationToken);
        }

        public Task<bool> TonTaiAsync(string? objectKey, CancellationToken cancellationToken = default) =>
            string.IsNullOrWhiteSpace(objectKey)
                ? Task.FromResult(false)
                : _storage.ExistsAsync(objectKey, cancellationToken);

        public Task RemoveAsync(string? objectKey, CancellationToken cancellationToken = default) =>
            string.IsNullOrWhiteSpace(objectKey)
                ? Task.CompletedTask
                : _storage.DeleteAsync(objectKey, cancellationToken);

        public Task RemoveAllAsync(int templateId, CancellationToken cancellationToken = default) =>
            _storage.DeleteByPrefixAsync(TemplateConstants.GetAssetKeyPrefix(templateId), cancellationToken);

        public void ValidateImage(IFormFile file)
        {
            if (file.Length <= 0)
            {
                throw new UserFriendlyException(ErrorCodes.TemplateImageInvalid,
                    $"File {file.FileName} rỗng.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!TemplateConstants.AllowedImageExtensions.Contains(extension))
            {
                throw new UserFriendlyException(ErrorCodes.TemplateImageInvalid,
                    $"Định dạng ảnh {extension} không được hỗ trợ. Chỉ nhận: "
                    + string.Join(", ", TemplateConstants.AllowedImageExtensions));
            }

            if (file.Length > TemplateConstants.MaxImageBytes)
            {
                throw new UserFriendlyException(ErrorCodes.TemplateImageInvalid,
                    $"Ảnh {file.FileName} nặng {file.Length / 1024 / 1024} MB, vượt mức cho phép "
                    + $"{TemplateConstants.MaxImageBytes / 1024 / 1024} MB.");
            }
        }
    }
}
