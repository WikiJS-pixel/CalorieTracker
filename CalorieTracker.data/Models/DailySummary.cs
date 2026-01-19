namespace CalorieTracker.Data.Models
{
    public class DailySummary
    {
        public DateTime Date { get; set; }
        public double TotalCalories { get; set; }
        public double TotalProtein { get; set; }
        public double TotalCarbs { get; set; }
        public double TotalFat { get; set; }
        public int MealCount { get; set; }
        public double TargetCalories { get; set; }
        public double TargetProtein { get; set; }
        public double RemainingCalories => Math.Max(0, TargetCalories - TotalCalories);
        public bool IsUnderTarget => TotalCalories <= TargetCalories;
    }
}
