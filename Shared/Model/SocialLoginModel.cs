using System.ComponentModel.DataAnnotations;

namespace Shared.Model
{
    public class SocialLoginModel
    {
        [Required]
        public SocialProvider Provider { get; set; }
        
        public string? ReturnUrl { get; set; }
    }
} 