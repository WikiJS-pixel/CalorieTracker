using System.ComponentModel.DataAnnotations;

namespace CalorieTracker.Data.Models
{
    public class MealEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserProfileId { get; set; }
        public UserProfile UserProfile { get; set; } = null!;

        public DateTime EntryDate { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public MealType MealType { get; set; } = MealType.Snack; // Breakfast, Lunch, Dinner, Snack

        [Required]
        public int FoodId { get; set; }
        public Food Food { get; set; } = null!;

        [Range(0.1, 5000, ErrorMessage = "Amount must be between 0.1 and 5000 grams")]
        public double AmountGrams { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
