using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksts.be.application.Auth.Dtos.User
{
    public class UpdateUserDto
    {
        private string _userName = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Không được bỏ trống")]
        required public string Id { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Không được bỏ trống")]
        public string UserName
        {
            get => _userName;
            set => _userName = value?.Trim() ?? string.Empty;
        }

        [EmailAddress]
        public string Email
        {
            get => _email;
            set => _email = value?.Trim() ?? string.Empty;
        }

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? FullName { get; set; }

        public List<string> RoleNames { get; set; } = new List<string>();
    }
}
