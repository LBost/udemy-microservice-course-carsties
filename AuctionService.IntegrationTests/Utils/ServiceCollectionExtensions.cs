using AuctionService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests.Utils
{
    public static class ServiceCollectionExtensions
    {
        public static void RemoveDbContext<TContext>(this IServiceCollection services)
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AuctionDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);
        }

        public static void InitializeDatabase<TContext>(this IServiceCollection services)
        {
            var sp = services.BuildServiceProvider();
            using var scoped = sp.CreateScope();
            var db = scoped.ServiceProvider.GetRequiredService<AuctionDbContext>();

            db.Database.Migrate();
            DbHelper.InitDbForTests(db);
        }
    }
}
