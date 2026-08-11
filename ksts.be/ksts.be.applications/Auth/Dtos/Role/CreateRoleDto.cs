using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksts.be.application.Auth.Dtos.Role
{
    public class CreateRoleDto
    {
        private string _name = String.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Không được bỏ trống")]
        public string Name { get => _name; set => _name = value.Trim(); }
        public List<string> PermissionKey { get; set; } = new List<string>();
    }
}
