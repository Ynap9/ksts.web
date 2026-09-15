namespace kssm.be.external.DongGoi.Interfaces
{
    /// <summary>
    /// Kho của luồng đóng gói. Bản NGUỒN tải lên tạm trú trên MinIO rồi dọn theo hạn giữ phiên; gói ZIP
    /// dựng xong đẩy thẳng lên Google Drive và chỉ còn sống ở đó.
    /// </summary>
    public interface IPackageFileStorage
    {
        Task<string> SaveSourceAsync(byte[] content, int sessionId, int sequence, string extension,
            string contentType, CancellationToken cancellationToken = default);

        Task<string> EnsurePackageFolderAsync(string tenThuMuc, CancellationToken cancellationToken = default);

        Task<string> SavePackageAsync(string driveFolderId, byte[] content, string tenFile,
            CancellationToken cancellationToken = default);

        Task<byte[]> DownloadAsync(string objectKey, CancellationToken cancellationToken = default);

        string BuildPackageUrl(string driveFileId);

        Task RemoveSessionAsync(int sessionId, CancellationToken cancellationToken = default);
    }
}
