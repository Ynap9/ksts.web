namespace kssm.be.external.DongGoi.Interfaces
{
    public interface IPackageFileStorage
    {
        Task<string> SaveSourceAsync(byte[] content, int sessionId, int sequence, string extension,
            string contentType, CancellationToken cancellationToken = default);

        Task<string> SavePackageAsync(byte[] content, int sessionId, string tenFile,
            CancellationToken cancellationToken = default);

        Task<byte[]> DownloadAsync(string objectKey, CancellationToken cancellationToken = default);

        string BuildPublicUrl(string objectKey);

        Task RemoveSessionAsync(int sessionId, CancellationToken cancellationToken = default);
    }
}
