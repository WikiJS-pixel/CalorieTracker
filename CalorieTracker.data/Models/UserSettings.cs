using System.ComponentModel.DataAnnotations;

namespace CalorieTracker.Data.Models
{
    public class UserSettings
    {
        [Key]
        public int Id { get; set; } = 1; // Tied to UserProfile.Id = 1

        // Macro distribution
        [Range(0, 100, ErrorMessage = "Protein percentage must be between 0 and 100")]
        public double ProteinPercentage { get; set; } = 25;

        [Range(0, 100, ErrorMessage = "Carbs percentage must be between 0 and 100")]
        public double CarbsPercentage { get; set; } = 50;

        [Range(0, 100, ErrorMessage = "Fat percentage must be between 0 and 100")]
        public double FatPercentage { get; set; } = 25;

        // Display preferences
        public bool UseMetricSystem { get; set; } = true;
        public string Theme { get; set; } = "Light";

        // Dietary preferences
        public bool TrackMacros { get; set; } = true;
        public bool TrackWater { get; set; } = false;

        [MaxLength(50)]
        public string? DietType { get; set; } // "Keto", "Low-carb", "Vegetarian", etc.

        // Notifications
        public TimeSpan MealReminderTime { get; set; } = new TimeSpan(12, 0, 0);
        public bool EnableMealReminders { get; set; } = true;

        public DateTime CreatedDate { get; set; } = new DateTime(2026, 1, 1);
        public DateTime? LastUpdatedDate { get; set; }
    }
}
