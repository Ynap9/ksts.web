using kssm.be.applications.DongGoi.Package.Dtos;

namespace kssm.be.applications.DongGoi.Package.Interfaces
{
    public interface IPackageBuilderService
    {
        Task<ViewDongGoiDto> DongGoiAsync(int sessionId, DongGoiDto dto,
            CancellationToken cancellationToken = default);

        Task<ViewDongGoiDto> DanhSachGoiAsync(int sessionId, CancellationToken cancellationToken = default);
    }
}
