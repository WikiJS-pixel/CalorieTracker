
using System.ComponentModel.DataAnnotations;

namespace CalorieTracker.Data.Models
{
    public class Food
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 2000)]
        public double CaloriesPer100g { get; set; }

        [Range(0, 100)]
        public double ProteinPer100g { get; set; }

        [Range(0, 100)]
        public double CarbsPer100g { get; set; }

        [Range(0, 100)]
        public double FatPer100g { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Computed properties (not stored in DB)
        public double CaloriesForAmount(double grams) => CaloriesPer100g * grams / 100;
        public double ProteinForAmount(double grams) => ProteinPer100g * grams / 100;
        public double CarbsForAmount(double grams) => CarbsPer100g * grams / 100;
        public double FatForAmount(double grams) => FatPer100g * grams / 100;
    }
}
