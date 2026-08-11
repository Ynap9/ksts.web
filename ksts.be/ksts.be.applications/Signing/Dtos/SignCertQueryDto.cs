using Microsoft.AspNetCore.Mvc;

namespace ksts.be.applications.Signing.Dtos
{
    public class SignCertQueryDto
    {
        [FromQuery(Name = "onlySignable")]
        public bool OnlySignable { get; set; }
    }
}
