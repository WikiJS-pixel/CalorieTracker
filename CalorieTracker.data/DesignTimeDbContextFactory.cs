using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Use a fixed dev path (same folder as project or a subfolder)
            var devPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dev_calorietracker.db");
            // Or match runtime for local testing: but AppDataDirectory not available at design-time

            optionsBuilder.UseSqlite($"Data Source={devPath}")
                .LogTo(Console.WriteLine, LogLevel.Information);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
