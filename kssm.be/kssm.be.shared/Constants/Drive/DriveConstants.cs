namespace kssm.be.shared.Constants.Drive
{
    public static class DriveConstants
    {
        public const string ConfigSection = "Drive";

        public const string ApplicationName = "kssm.be";

        public const string FolderMimeType = "application/vnd.google-apps.folder";

        public const string FolderUrlFormat = "https://drive.google.com/drive/folders/{0}";

        public const string FileUrlFormat = "https://drive.google.com/file/d/{0}/view";

        public const string FolderQueryFormat =
            "mimeType = '{0}' and name = '{1}' and '{2}' in parents and trashed = false";

        public const string FolderQueryFields = "files(id)";

        public const string UploadFields = "id";

        public const int UploadMaxAttempts = 4;

        public const int UploadRetryDelaySeconds = 2;

        public static readonly string[] RateLimitReasons = { "rateLimitExceeded", "userRateLimitExceeded" };

        public static string GetFolderUrl(string folderId) => string.Format(FolderUrlFormat, folderId);

        public static string GetFileUrl(string fileId) => string.Format(FileUrlFormat, fileId);

        public static string EscapeQueryValue(string value) =>
            value.Replace("\\", "\\\\").Replace("'", "\\'");

        public static string? ChuanHoaTenThuMuc(string? ten)
        {
            if (string.IsNullOrWhiteSpace(ten))
            {
                return null;
            }

            var sach = new string(ten.Where(x => x != '/' && x != '\\' && !char.IsControl(x)).ToArray()).Trim();

            return string.IsNullOrEmpty(sach) ? null : sach;
        }
    }
}
