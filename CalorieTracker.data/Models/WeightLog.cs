using System.ComponentModel.DataAnnotations;

namespace CalorieTracker.Data.Models
{
    public class WeightLog
    {
        [Key]
        public int Id { get; set; }

        [Range(20, 300, ErrorMessage = "Weight must be between 20 and 300 kg")]
        public double WeightKg { get; set; }

        public DateTime LogDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
