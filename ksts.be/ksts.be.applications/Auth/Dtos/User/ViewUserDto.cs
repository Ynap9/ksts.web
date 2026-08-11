using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksts.be.application.Auth.Dtos.User
{
    public class ViewUserDto
    {
        public string Id { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? PhoneNumber { get; set; }
        //public string? MsAccount { get; set; }
        public string? FullName { get; set; }
        public string? PasswordRandom { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }

        // Optional: include roles
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
