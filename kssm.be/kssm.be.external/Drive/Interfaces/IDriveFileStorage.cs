using kssm.be.external.Drive.Dtos;

namespace kssm.be.external.Drive.Interfaces
{
    public interface IDriveFileStorage
    {
        Task<DriveFolderDto> EnsureFolderAsync(string rootFolderId, string tenThuMuc,
            CancellationToken cancellationToken = default);

        Task<string> UploadAsync(string folderId, byte[] content, string tenFile, string contentType,
            CancellationToken cancellationToken = default);
    }
}
