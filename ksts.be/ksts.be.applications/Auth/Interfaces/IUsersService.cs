using ksts.be.application.Auth.Dtos.User;
using ksts.be.domain.Auth;
using ksts.be.shared.HttpRequest.BaseRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using thongbao.be.application.Auth.Dtos.User;

namespace ksts.be.application.Auth.Interfaces
{
    public interface IUsersService
    {
        public Task<ViewUserDto> Create(CreateUserDto dto);
        public Task Update(UpdateUserDto dto);
        public Task<BaseResponsePagingDto<ViewUserDto>> FindPaging(FindPagingUserDto dto);
        public Task<ViewUserDto> FindById(string id);
        //public Task<ViewUserDto> FindByMsAccount(string msAccount);
        public Task SetRoleForUser(SetRoleForUserDto dto);
        public Task<ViewMeDto> GetMe();
        public List<GetListUserDto> GetListUser();
    }
}
