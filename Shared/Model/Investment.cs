using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Model
{
    public class Investment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int PropertyId { get; set; }

        [Required]
        public int Shares { get; set; }

        [Required]
        public decimal TotalInvestment { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("PropertyId")]
        public Property Property { get; set; } = null!;
    }
} 