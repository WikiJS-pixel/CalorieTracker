using CalorieTracker.Data.Models;

namespace CalorieTracker.Data.DTOs
{
    public class DailySummaryWithGoals
    {
        public DailySummary Summary { get; set; } = null!;
        public DailyGoals Goals { get; set; } = null!;
    }
}
