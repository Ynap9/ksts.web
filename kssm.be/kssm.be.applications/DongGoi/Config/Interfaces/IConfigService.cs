using kssm.be.applications.DongGoi.Config.Dtos;
using kssm.be.shared.Requests.BaseRequest;

namespace kssm.be.applications.DongGoi.Config.Interfaces
{
    public interface IConfigService
    {
        Task<BaseResponsePagingDto<ViewPackageFieldDto>> FindPagingAsync(FindPagingPackageFieldDto input);
        Task<GetDropDownTypeTT05Dto> GetDropDownTypeTT05Async();
        Task<ExportFileDto> ExportExcelAsync(ExportPackageFieldDto input);
    }
}
