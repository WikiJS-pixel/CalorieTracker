namespace CalorieTracker.data.Interfaces
{
    public interface IDbContextFactory
    {
        AppDbContext CreateContext();
    }
}
