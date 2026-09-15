using Microsoft.Extensions.Configuration;

namespace kssm.be.shared.Settings
{
    public class DriveSettings
    {
        [ConfigurationKeyName("DRIVE_CREDENTIALS_PATH")]
        public string CredentialsPath { get; set; } = string.Empty;

        [ConfigurationKeyName("DRIVE_SIGN_FOLDER_ID")]
        public string SignFolderId { get; set; } = string.Empty;

        [ConfigurationKeyName("DRIVE_PACKAGE_FOLDER_ID")]
        public string PackageFolderId { get; set; } = string.Empty;
    }
}
