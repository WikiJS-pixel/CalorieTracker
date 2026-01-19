namespace CalorieTracker.Data.DTOs
{
    public class UserProgress
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double TotalCalories { get; set; }
        public double TotalProtein { get; set; }
        public double? StartingWeight { get; set; }
        public double? EndingWeight { get; set; }
        public double? WeightChange { get; set; }
        public double AverageDailyCalories { get; set; }
    }
}
