using kssm.be.shared.Requests.BaseRequest;
using Microsoft.AspNetCore.Mvc;

namespace kssm.be.applications.KySo.Signing.Dtos
{
    public class SignCertQueryDto : BaseRequestPagingDto
    {
        [FromQuery(Name = "onlySignable")]
        public bool OnlySignable { get; set; }
    }
}
