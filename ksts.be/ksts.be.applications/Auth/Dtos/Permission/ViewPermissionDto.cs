using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksts.be.application.Auth.Dtos.Permission
{
    public class ViewPermissionDto
    {
        public string Key { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public string Category { get; set; } = String.Empty;
    }
}
