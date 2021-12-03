using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace LODFinals.Definitions.Models
{
    public class LoginForm
    {
        [Required]
        public string Login { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool IsRememberMe { get; set; }
    }
}
