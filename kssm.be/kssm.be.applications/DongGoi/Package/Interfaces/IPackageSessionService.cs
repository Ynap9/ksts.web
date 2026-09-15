using kssm.be.applications.DongGoi.Package.Dtos;
using Microsoft.AspNetCore.Http;

namespace kssm.be.applications.DongGoi.Package.Interfaces
{
    public interface IPackageSessionService
    {
        Task<ViewPhienDto> TaoPhienAsync(TaoPhienDto input, CancellationToken cancellationToken = default);

        Task<ViewPhienDto> ThemFileAsync(int sessionId, IFormFileCollection files,
            IReadOnlyList<string> duongDan, CancellationToken cancellationToken = default);

        Task<ViewPhienDto> BatDauAsync(int sessionId, CancellationToken cancellationToken = default);

        Task<ViewTienDoKiemTraDto> TienDoAsync(int sessionId, CancellationToken cancellationToken = default);

        Task<ViewKetQuaKiemTraDto> KetQuaAsync(int sessionId, CancellationToken cancellationToken = default);

        Task HuyAsync(int sessionId, CancellationToken cancellationToken = default);

        Task DonPhienQuaHanAsync(int soGioGiu, CancellationToken cancellationToken = default);
    }
}
