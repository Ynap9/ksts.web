using Amazon.S3;
using kssm.be.external.S3.Dtos;
using kssm.be.external.S3.Interfaces;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;
using kssm.be.shared.Settings;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace kssm.be.external.S3.Implements
{
    public class S3ClientFactory : IS3ClientFactory
    {
        private readonly S3Settings _settings;
        private readonly ConcurrentDictionary<string, IAmazonS3> _clients = new();

        public S3ClientFactory(IOptions<S3Settings> options)
        {
            _settings = options.Value;
        }

        public IAmazonS3 Get(S3ConnectionDto connection)
        {
            if (string.IsNullOrWhiteSpace(connection.Url) || string.IsNullOrWhiteSpace(connection.Bucket))
            {
                throw new UserFriendlyException(ErrorCodes.StorageNotConfigured);
            }

            var cacheKey = $"{connection.Url}|{connection.Bucket}|{connection.AccessKey}";

            return _clients.GetOrAdd(cacheKey, _ => new AmazonS3Client(connection.AccessKey, connection.SecretKey,
                new AmazonS3Config
                {
                    ServiceURL = connection.Url,
                    // MinIO không phục vụ theo tên miền con của bucket như S3 thật, phải ghép bucket vào đường dẫn.
                    ForcePathStyle = true,
                    AuthenticationRegion = connection.Region,
                    UseHttp = !connection.WithSSL,
                }));
        }

        public S3ConnectionDto DefaultConnection() => new()
        {
            Url = _settings.Url,
            Region = _settings.Region,
            Bucket = _settings.Bucket,
            AccessKey = _settings.AccessKey,
            SecretKey = _settings.SecretKey,
            WithSSL = _settings.WithSSL,
        };
    }
}
