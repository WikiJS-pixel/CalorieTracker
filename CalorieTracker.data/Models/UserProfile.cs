using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalorieTracker.Data.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; } = 1; // Always 1

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [Range(50, 250, ErrorMessage = "Height must be between 50 and 250 cm")]
        public double HeightCm { get; set; }

        [Required]
        public ActivityLevel ActivityLevel { get; set; }

        [Required]
        public WeightGoal WeightGoal { get; set; }

        [Range(-2.0, 2.0, ErrorMessage = "Weight change rate must be between -2 and 2 kg/week")]
        public double WeightChangeRateKgPerWeek { get; set; } = 0.5; // Default 0.5 kg/week

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdatedDate { get; set; }

        public ICollection<WeightLog> WeightLogs { get; set; } = [];

        [NotMapped]
        public double? CurrentWeightKg => WeightLogs?.OrderByDescending(w => w.LogDate)
                             .FirstOrDefault()?.WeightKg;
    }
}
