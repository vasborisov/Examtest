using proekt_za_6ca.Data.Entities;

namespace proekt_za_6ca.Data.Seeders
{
    /// <summary>
    /// Seeds demo data
    /// </summary>
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IServiceProvider scopeProvider = scope.ServiceProvider;
            ApplicationDbContext dbContext = scopeProvider.GetRequiredService<ApplicationDbContext>();
            if (!dbContext.Restaurants.Any())
            {

                await UserSeeder.SeedAsync(serviceProvider, dbContext);
                await RestaurantSeeder.SeedAsync(serviceProvider, dbContext);
            } 
           
        }
    }
}
