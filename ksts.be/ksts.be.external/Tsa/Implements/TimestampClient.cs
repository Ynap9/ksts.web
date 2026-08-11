using ksts.be.external.Tsa.Interfaces;
using ksts.be.shared.Constants.Signing;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests.ErrorRequest;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;

namespace ksts.be.external.Tsa.Implements
{
    /// <summary>
    /// Client RFC 3161 dựng bằng System.Security.Cryptography.Pkcs của BCL, không cần thư viện ngoài.
    /// </summary>
    public class TimestampClient : ITimestampClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TimestampClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc/>
        public async Task<byte[]> RequestTokenAsync(byte[] signatureValue, CancellationToken cancellationToken)
        {
            var hash = SHA256.HashData(signatureValue);
            var delay = TimeSpan.FromSeconds(SigningConstants.TsaRetryDelaySeconds);
            Exception? loiCuoi = null;

            for (var lan = 1; lan <= SigningConstants.TsaMaxAttempts; lan++)
            {
                try
                {
                    return await LayTokenAsync(hash, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    loiCuoi = ex;
                }

                if (lan < SigningConstants.TsaMaxAttempts)
                {
                    await Task.Delay(delay, cancellationToken);
                    delay += delay;
                }
            }

            throw new UserFriendlyException(ErrorCodes.TimestampFailed,
                $"Không lấy được dấu thời gian sau {SigningConstants.TsaMaxAttempts} lần thử: {loiCuoi?.Message}");
        }

        /// <summary>Một lượt gọi TSA. Tách riêng để vòng thử lại chỉ lo việc đếm lần và giãn cách.</summary>
        public async Task<byte[]> LayTokenAsync(byte[] hash, CancellationToken cancellationToken)
        {
            // nonce chống replay: ProcessResponse đối chiếu lại nên token của request khác sẽ bị bắt.
            // requestSignerCertificates cho TSA nhúng chain của nó vào token, nhờ đó về sau verify được offline.
            var request = Rfc3161TimestampRequest.CreateFromHash(
                hash,
                HashAlgorithmName.SHA256,
                requestedPolicyId: null,
                nonce: RandomNumberGenerator.GetBytes(8),
                requestSignerCertificates: true);

            using var content = new ByteArrayContent(request.Encode());
            content.Headers.ContentType = new MediaTypeHeaderValue(SigningConstants.TsaContentType);

            var client = _httpClientFactory.CreateClient(nameof(TimestampClient));
            client.Timeout = TimeSpan.FromSeconds(SigningConstants.TsaTimeoutSeconds);

            using var response = await client.PostAsync(SigningConstants.TsaUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"TSA trả về HTTP {(int)response.StatusCode}.");
            }

            var reply = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return request.ProcessResponse(reply, out _).AsSignedCms().Encode();
        }

        /// <inheritdoc/>
        public DateTime? DocGenTime(byte[] token)
        {
            if (!Rfc3161TimestampToken.TryDecode(token, out var decoded, out _) || decoded == null)
            {
                return null;
            }

            return decoded.TokenInfo.Timestamp.UtcDateTime;
        }
    }
}
