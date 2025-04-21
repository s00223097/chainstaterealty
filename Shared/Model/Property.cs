using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Model
{
    public class Property
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int TotalShares { get; set; }

        [Required]
        public decimal SharePrice { get; set; }

        [Required]
        public int AvailableShares { get; set; }

        public List<string> ImageUrls { get; set; } = new();

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string OwnerId { get; set; } = string.Empty;
    }
} 