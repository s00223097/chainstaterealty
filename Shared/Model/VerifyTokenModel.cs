using System.ComponentModel.DataAnnotations;

namespace Shared.Model
{
    public class VerifyTokenModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
} 