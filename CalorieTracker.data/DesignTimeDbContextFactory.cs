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

            // Use a simple connection string for design-time
            const string connectionString = "Data Source=calorietracker.db";

            optionsBuilder.UseSqlite(connectionString)
                .LogTo(Console.WriteLine, LogLevel.Information);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
