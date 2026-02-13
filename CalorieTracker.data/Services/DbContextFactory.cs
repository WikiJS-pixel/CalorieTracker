using CalorieTracker.data.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CalorieTracker.data.Services
{
    public class DbContextFactory : IDbContextFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DbContextFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public AppDbContext CreateContext()
        {
            // Create a new scope and get a fresh DbContext
            var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }
    }
}
