using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kssm.be.external.S3.Dtos
{
    public class S3UploadResultDto
    {
        public string ObjectKey { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long Length { get; set; }
    }
}
