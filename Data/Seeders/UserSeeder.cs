using Microsoft.AspNetCore.Identity;
using proekt_za_6ca.Data.Entities;

namespace proekt_za_6ca.Data.Seeders
{
    class UserSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, ApplicationDbContext dbContext)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            if (!userManager.Users.Any())
            {
                await SeedRoles(roleManager);
                await SeedUsers(userManager);
            }
        }

        private static async Task SeedUsers(UserManager<User> userManager)
        {
            var adminUser = new User
            {
                UserName = "admin@admin.com",
                Email = "admin@admin.com",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "Admin"
            };
            string adminPassword = "Test#123";
            await SeedUser(adminUser, adminPassword, "Admin", userManager);

            var ownerUser = new User
            {
                UserName = "owner1@owner.com",
                Email = "owner1@owner.com",
                EmailConfirmed = true,
                FirstName = "Owner",
                LastName = "User 1"

            };
            string ownerPassword = "Test#123";
            await SeedUser(ownerUser, ownerPassword, "RestaurantOwner", userManager);

            var ownerUser2 = new User
            {
                UserName = "owner2@owner.com",
                Email = "owner2@owner.com",
                EmailConfirmed = true,
                FirstName = "Owner",
                LastName = "User 2"
            };
            string ownerPassword2 = "Test#123";
            await SeedUser(ownerUser2, ownerPassword2, "RestaurantOwner", userManager);

            var user = new User
            {
                UserName = "user@user.com",
                Email = "user@user.com",
                EmailConfirmed = true,
                FirstName = "Regular",
                LastName = "User"
            };
            string userPassword = "Test#123";
            await SeedUser(user, userPassword, "User", userManager);
        }

        private static async Task SeedUser(User user, string password, string roleName,
           UserManager<User> userManager)
        {
            var userInfo = await userManager.FindByEmailAsync(user.Email);
            if (userInfo == null)
            {
                var created = await userManager
                    .CreateAsync(user, password);
                if (created.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, roleName);
                }
            }
        }
        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Admin", "RestaurantOwner", "User" };
            foreach (var role in roleNames)
            {
                bool roleExist = await roleManager.RoleExistsAsync(role);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

       
    }
}
