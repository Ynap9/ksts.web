using kssm.be.applications.DongGoi.Package.Dtos;

namespace kssm.be.applications.DongGoi.Package.Interfaces
{
    public interface IPackageBuildRunnerService
    {
        void BatDau(int sessionId, DongGoiDto dto);

        void Dung(int sessionId);

        bool DangChay(int sessionId);

        Task ChayAsync(int sessionId, DongGoiDto dto, CancellationToken cancellationToken);
    }
}
