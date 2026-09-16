namespace kssm.be.external.DongGoi.Interfaces
{
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
