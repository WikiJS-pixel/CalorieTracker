using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalorieTracker.Data.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        public UserProfile()
        {
            Id = 1; // Force single user ID
        }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "Default User";

        public DateTime BirthDate { get; set; } = new DateTime(1990, 1, 1);

        [Required]
        public Gender Gender { get; set; } = Gender.Other;

        [Range(50, 250, ErrorMessage = "Height must be between 50 and 250 cm")]
        public double HeightCm { get; set; } = 170;

        [Required]
        public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.ModeratelyActive;

        [Required]
        public WeightGoal WeightGoal { get; set; } = WeightGoal.Maintain;

        [Range(-2.0, 2.0, ErrorMessage = "Weight change rate must be between -2 and 2 kg/week")]
        public double WeightChangeRateKgPerWeek { get; set; } = 0.5; // Default 0.5 kg/week

        public DateTime CreatedDate { get; set; } 
        public DateTime? LastUpdatedDate { get; set; }

        public UserSettings? UserSettings { get; set; }
    }
}
