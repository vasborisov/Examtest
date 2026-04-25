using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using proekt_za_6ca.Data;
using proekt_za_6ca.Data.Entities;
using proekt_za_6ca.Services;
using proekt_za_6ca.ViewModels;

namespace proekt_za_6ca.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IImageService _imageService;

        public AdminController(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IImageService imageService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _imageService = imageService;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TotalRestaurants = await _context.Restaurants.CountAsync();
            ViewBag.TotalReservations = await _context.Reservations.CountAsync();
            ViewBag.PendingReservations = await _context.Reservations.CountAsync(r => r.Status == Data.Enums.ReservationStatus.Pending);
            
            return View();
        }

        // User Management
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserWithRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserWithRoleViewModel
                {
                    User = user,
                    Roles = roles
                });
            }

            return View(userViewModels);
        }

        // GET: Admin/CreateUser
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.Roles = new SelectList(await _roleManager.Roles
                .Where(r => r.Name != "Admin")
                .ToListAsync(), "Name", "Name");
            return View();
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            try
            {
                // Additional validation
                if (string.IsNullOrWhiteSpace(model.FirstName))
                {
                    ModelState.AddModelError(nameof(model.FirstName), "First name is required.");
                }

                if (string.IsNullOrWhiteSpace(model.LastName))
                {
                    ModelState.AddModelError(nameof(model.LastName), "Last name is required.");
                }

                if (string.IsNullOrWhiteSpace(model.Role) || model.Role == "Admin")
                {
                    ModelState.AddModelError(nameof(model.Role), "Please select a valid role.");
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "A user with this email address already exists.");
                }

                // Validate password strength
                if (model.Password.Length < 6)
                {
                    ModelState.AddModelError(nameof(model.Password), "Password must be at least 6 characters long.");
                }

                if (!model.Password.Any(char.IsDigit))
                {
                    ModelState.AddModelError(nameof(model.Password), "Password must contain at least one number.");
                }

                if (!model.Password.Any(char.IsLetter))
                {
                    ModelState.AddModelError(nameof(model.Password), "Password must contain at least one letter.");
                }

                if (ModelState.IsValid)
                {
                    var user = new User
                    {
                        UserName = model.Email.Trim().ToLower(),
                        Email = model.Email.Trim().ToLower(),
                        FirstName = model.FirstName.Trim(),
                        LastName = model.LastName.Trim(),
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                        TempData["Success"] = $"User '{user.Email}' has been created successfully with role '{model.Role}'.";
                        return RedirectToAction(nameof(Users));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while creating the user. Please try again.");
            }

            ViewBag.Roles = new SelectList(await _roleManager.Roles
                .Where(r => r.Name != "Admin")
                .ToListAsync(), "Name", "Name", model.Role);
            return View(model);
        }

        // GET: Admin/EditUser/5
        public async Task<IActionResult> EditUser(string id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = userRoles.FirstOrDefault() ?? "User"
            };

            ViewBag.Roles = new SelectList(await _roleManager.Roles
                .Where(r => r.Name != "Admin")
                .ToListAsync(), "Name", "Name", model.Role);
            return View(model);
        }

        // POST: Admin/EditUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, EditUserViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound();

                user.Email = model.Email;
                user.UserName = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, model.Role);

                    return RedirectToAction(nameof(Users));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new SelectList(await _roleManager.Roles
                .Where(r => r.Name != "Admin")
                .ToListAsync(), "Name", "Name", model.Role);
            return View(model);
        }

        // GET: Admin/DeleteUser/5
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var viewModel = new UserWithRoleViewModel
            {
                User = user,
                Roles = userRoles
            };

            return View(viewModel);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["Error"] = "Invalid user ID provided.";
                    return RedirectToAction(nameof(Users));
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Users));
                }

                // Check if user is an admin
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    TempData["Error"] = "Cannot delete administrator users.";
                    return RedirectToAction(nameof(Users));
                }

                // Check if user has restaurants or reservations
                var hasRestaurants = await _context.Restaurants.AnyAsync(r => r.OwnerId == id);
                var hasReservations = await _context.Reservations.AnyAsync(r => r.OwnerId == id);

                if (hasRestaurants)
                {
                    TempData["Error"] = $"Cannot delete user '{user.Email}' because they own restaurants. Please reassign or delete the restaurants first.";
                    return RedirectToAction(nameof(Users));
                }

                if (hasReservations)
                {
                    TempData["Error"] = $"Cannot delete user '{user.Email}' because they have existing reservations. Please handle the reservations first.";
                    return RedirectToAction(nameof(Users));
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["Success"] = $"User '{user.Email}' has been deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to delete user. " + string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the user. Please try again.";
            }

            return RedirectToAction(nameof(Users));
        }

        // Restaurant Management
        public async Task<IActionResult> Restaurants()
        {
            var restaurants = await _context.Restaurants
                .Include(r => r.Owner)
                .ToListAsync();
            return View(restaurants);
        }

        // GET: Admin/CreateRestaurant
        public async Task<IActionResult> CreateRestaurant()
        {
            var restaurantOwners = await _userManager.GetUsersInRoleAsync("RestaurantOwner");
            ViewBag.RestaurantOwners = new SelectList(restaurantOwners, "Id", "Email");
            return View(new RestaurantCreateViewModel());
        }

        // POST: Admin/CreateRestaurant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRestaurant(RestaurantCreateViewModel model)
        {
            try
            {
                // Validate that either ImageFile or ImageUrl is provided
                if (model.ImageFile == null && string.IsNullOrEmpty(model.ImageUrl))
                {
                    ModelState.AddModelError("", "Please either upload an image or provide an image URL.");
                }

                // Validate image file if provided
                if (model.ImageFile != null && !_imageService.IsValidImage(model.ImageFile))
                {
                    ModelState.AddModelError(nameof(model.ImageFile), "Please upload a valid image file (JPG, PNG, GIF, WebP) under 2MB.");
                }

                if (ModelState.IsValid)
                {
                    var restaurant = new Restaurants
                    {
                        Id = Guid.NewGuid(),
                        Title = model.Title,
                        Description = model.Description,
                        Address = model.Address,
                        Latitude = model.Latitude,
                        Longitude = model.Longitude,
                        OwnerId = model.OwnerId,
                        CreatedOn = DateTime.Now
                    };

                    // Handle image upload or URL
                    if (model.ImageFile != null)
                    {
                        restaurant.ImageUrl = await _imageService.SaveImageAsync(model.ImageFile, Request);
                    }
                    else if (!string.IsNullOrEmpty(model.ImageUrl))
                    {
                        restaurant.ImageUrl = model.ImageUrl;
                    }

                    _context.Add(restaurant);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "Restaurant created successfully!";
                    return RedirectToAction(nameof(Restaurants));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating the restaurant. Please try again.");
            }

            var restaurantOwners = await _userManager.GetUsersInRoleAsync("RestaurantOwner");
            ViewBag.RestaurantOwners = new SelectList(restaurantOwners, "Id", "Email", model.OwnerId);
            return View(model);
        }

        // GET: Admin/EditRestaurant/5
        public async Task<IActionResult> EditRestaurant(Guid? id)
        {
            if (id == null)
                return NotFound();

            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null)
                return NotFound();

            var restaurantOwners = await _userManager.GetUsersInRoleAsync("RestaurantOwner");
            ViewBag.RestaurantOwners = new SelectList(restaurantOwners, "Id", "Email", restaurant.OwnerId);
            return View(restaurant);
        }

        // POST: Admin/EditRestaurant/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRestaurant(Guid id, Restaurants restaurant)
        {
            if (id != restaurant.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(restaurant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RestaurantExists(restaurant.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Restaurants));
            }

            var restaurantOwners = await _userManager.GetUsersInRoleAsync("RestaurantOwner");
            ViewBag.RestaurantOwners = new SelectList(restaurantOwners, "Id", "Email", restaurant.OwnerId);
            return View(restaurant);
        }

        // GET: Admin/DeleteRestaurant/5
        public async Task<IActionResult> DeleteRestaurant(Guid? id)
        {
            if (id == null)
                return NotFound();

            var restaurant = await _context.Restaurants
                .Include(r => r.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (restaurant == null)
                return NotFound();

            return View(restaurant);
        }

        // POST: Admin/DeleteRestaurant/5
        [HttpPost, ActionName("DeleteRestaurant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRestaurantConfirmed(Guid id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant != null)
            {
                // Check if restaurant has reservations
                var hasReservations = await _context.Reservations.AnyAsync(r => r.RestaurantId == id);
                if (hasReservations)
                {
                    TempData["Error"] = "Cannot delete restaurant with existing reservations.";
                    return RedirectToAction(nameof(Restaurants));
                }

                _context.Restaurants.Remove(restaurant);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Restaurants));
        }

        private bool RestaurantExists(Guid id)
        {
            return _context.Restaurants.Any(e => e.Id == id);
        }
    }

}