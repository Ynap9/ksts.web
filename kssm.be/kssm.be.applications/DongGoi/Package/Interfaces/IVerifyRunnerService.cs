namespace kssm.be.applications.DongGoi.Package.Interfaces
{
    public interface IVerifyRunnerService
    {
        void BatDau(int sessionId);

        bool DangChay(int sessionId);

        Task ChayAsync(int sessionId, CancellationToken cancellationToken);
    }
}
