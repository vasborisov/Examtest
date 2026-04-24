using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using proekt_za_6ca.Data.Entities;

namespace proekt_za_6ca.Data.Seeders
{
    public class RestaurantSeeder
    {
        public static async Task<List<Restaurants>> SeedAsync(IServiceProvider serviceProvider,ApplicationDbContext dbContext)
        {

            if (await dbContext.Restaurants.AnyAsync())
            {
                return await dbContext.Restaurants.ToListAsync();
            }
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            var ownerUser1 = await userManager.FindByEmailAsync("owner1@owner.com");
            if (ownerUser1 == null )
            {
                throw new InvalidOperationException("Owners does not exist");
            }
            var ownerUser2 = await userManager.FindByEmailAsync("owner2@owner.com");
            if (ownerUser2 == null )
            {
                throw new InvalidOperationException("Owner does not exist");
            }
            List<Restaurants> restaurants = [
                new Restaurants { Id = Guid.NewGuid(), Title = "Sunset Restaurant", Address = "Slaveykov 31", Description = "A nice restaurant", ImageUrl= "restaurant.jpg", Latitude = 42.5264924 , Longitude = 27.3695658, OwnerId = ownerUser1.Id },
                 new Restaurants { Id = Guid.NewGuid(), Title = "Sunrise Restaurant", Address = "Izgrev 25", Description = "A nice restaurant", ImageUrl= "restaurant2.jpg", Latitude = 42.5264924 , Longitude = 27.3695658, OwnerId = ownerUser2.Id },
                  new Restaurants { Id = Guid.NewGuid(), Title = "Downtown vibes", Address = "City center", Description = "A nice restaurant", ImageUrl= "restaurant3.jpg", Latitude = 42.5264924 , Longitude = 27.3695658, OwnerId = ownerUser1.Id },
            ];

            await dbContext.Restaurants.AddRangeAsync(restaurants);
            await dbContext.SaveChangesAsync();

            

            return restaurants;
        }
    }
}
