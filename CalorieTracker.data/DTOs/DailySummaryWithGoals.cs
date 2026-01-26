using CalorieTracker.data.Models;

namespace CalorieTracker.data.DTOs
{
    public class DailySummaryWithGoals
    {
        public DailySummary Summary { get; set; } = null!;
        public DailyGoals Goals { get; set; } = null!;
    }
}
