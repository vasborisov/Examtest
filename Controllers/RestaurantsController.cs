using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
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
    public class RestaurantsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IImageService _imageService;

        public RestaurantsController(ApplicationDbContext context, UserManager<User> userManager, IImageService imageService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
        }

        // GET: Restaurants
        public async Task<IActionResult> Index(string? search, string? location, string? sortBy)
        {
            var restaurants = _context.Restaurants.Include(r => r.Owner).AsQueryable();
            
            // Search by name or description
            if (!string.IsNullOrEmpty(search))
            {
                restaurants = restaurants.Where(r => 
                    r.Title.Contains(search) || 
                    r.Description.Contains(search) || 
                    r.Address.Contains(search));
                ViewBag.SearchTerm = search;
            }

            // Filter by location/address
            if (!string.IsNullOrEmpty(location))
            {
                restaurants = restaurants.Where(r => r.Address.Contains(location));
                ViewBag.LocationFilter = location;
            }

            // Sort results
            restaurants = sortBy switch
            {
                "name_desc" => restaurants.OrderByDescending(r => r.Title),
                "location" => restaurants.OrderBy(r => r.Address),
                "location_desc" => restaurants.OrderByDescending(r => r.Address),
                "newest" => restaurants.OrderByDescending(r => r.CreatedOn),
                "oldest" => restaurants.OrderBy(r => r.CreatedOn),
                _ => restaurants.OrderBy(r => r.Title), // Default: name ascending
            };
            ViewBag.CurrentSort = sortBy;

            // Provide sorting options for the view
            ViewBag.NameSort = string.IsNullOrEmpty(sortBy) ? "name_desc" : "";
            ViewBag.LocationSort = sortBy == "location" ? "location_desc" : "location";
            ViewBag.DateSort = sortBy == "newest" ? "oldest" : "newest";

            var restaurantList = await restaurants.ToListAsync();
            ViewBag.TotalResults = restaurantList.Count;
            
            return View(restaurantList);
        }

        // GET: Search suggestions (for AJAX)
        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            var suggestions = await _context.Restaurants
                .Where(r => r.Title.Contains(term) || r.Description.Contains(term) || r.Address.Contains(term))
                .Select(r => new { 
                    id = r.Id,
                    title = r.Title, 
                    address = r.Address,
                    description = r.Description.Substring(0, Math.Min(r.Description.Length, 50)) + "..."
                })
                .Take(10)
                .ToListAsync();

            return Json(suggestions);
        }

        // GET: Restaurants/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants
                .Include(r => r.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (restaurant == null)
            {
                return NotFound();
            }

            // Create reservation model for the form
            var reservationModel = new Reservation
            {
                RestaurantId = restaurant.Id,
                ReservationTime = DateTime.Now.AddDays(1), // Default to tomorrow
                PeopleCount = 2 // Default value
            };

            ViewBag.ReservationModel = reservationModel;
            ViewBag.IsAuthenticated = User.Identity!.IsAuthenticated;
            
            return View(restaurant);
        }

        // POST: Make Reservation
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeReservation(Guid restaurantId, DateTime reservationTime, int peopleCount, string comment = "")
        {
            try
            {
                var restaurant = await _context.Restaurants.FindAsync(restaurantId);
                if (restaurant == null)
                {
                    TempData["Error"] = "Restaurant not found. Please try again.";
                    return RedirectToAction("Index");
                }

                // Comprehensive validation
                var validationErrors = new List<string>();

                // Validate reservation time
                if (reservationTime <= DateTime.Now)
                {
                    validationErrors.Add("Reservation time must be in the future.");
                }

                if (reservationTime > DateTime.Now.AddYears(1))
                {
                    validationErrors.Add("Reservations can only be made up to 1 year in advance.");
                }

                // Validate people count
                if (peopleCount < 1)
                {
                    validationErrors.Add("Number of people must be at least 1.");
                }

                if (peopleCount > 20)
                {
                    validationErrors.Add("Maximum 20 people per reservation. For larger groups, please contact the restaurant directly.");
                }

                // Validate comment length
                if (!string.IsNullOrEmpty(comment) && comment.Length > 500)
                {
                    validationErrors.Add("Comment cannot exceed 500 characters.");
                }

                // Check for business hours (example: 9 AM to 11 PM)
                var hour = reservationTime.Hour;
                if (hour < 9 || hour > 23)
                {
                    validationErrors.Add("Reservations are only available between 9:00 AM and 11:00 PM.");
                }

                // Check if user already has a pending reservation at the same restaurant for the same day
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var existingReservation = await _context.Reservations.AnyAsync(r => 
                    r.OwnerId == userId && 
                    r.RestaurantId == restaurantId && 
                    r.ReservationTime.Date == reservationTime.Date &&
                    r.Status == Data.Enums.ReservationStatus.Pending);

                if (existingReservation)
                {
                    validationErrors.Add("You already have a pending reservation at this restaurant for the selected date.");
                }

                if (validationErrors.Any())
                {
                    TempData["Error"] = string.Join(" ", validationErrors);
                    return RedirectToAction("Details", new { id = restaurantId });
                }

                var reservation = new Reservation
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    ReservationTime = reservationTime,
                    PeopleCount = peopleCount,
                    Comment = comment?.Trim() ?? string.Empty,
                    OwnerId = userId,
                    Status = Data.Enums.ReservationStatus.Pending,
                    CreatedOn = DateTime.Now
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Your reservation has been submitted successfully and is pending approval from the restaurant.";
                return RedirectToAction("Details", new { id = restaurantId });
            }
            catch (Exception ex)
            {
                // Log the error (in a real app, use proper logging)
                TempData["Error"] = "An error occurred while processing your reservation. Please try again.";
                return RedirectToAction("Details", new { id = restaurantId });
            }
        }

        // GET: Restaurants/Create
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> Create()
        {
            var model = new RestaurantCreateViewModel();
            
            if (User.IsInRole("RestaurantOwner"))
            {
                // Restaurant owners can only create restaurants for themselves
                var currentUser = await _userManager.GetUserAsync(User);
                model.OwnerId = currentUser?.Id ?? "";
                ViewData["OwnerId"] = new SelectList(new[] { currentUser }, "Id", "Email", currentUser?.Id);
            }
            else
            {
                // Admins can assign to any restaurant owner
                var restaurantOwners = await _userManager.GetUsersInRoleAsync("RestaurantOwner");
                ViewData["OwnerId"] = new SelectList(restaurantOwners, "Id", "Email");
            }
            return View(model);
        }

        // POST: Restaurants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> Create(RestaurantCreateViewModel model)
        {
            try
            {
                // If user is RestaurantOwner, force OwnerId to be themselves
                if (User.IsInRole("RestaurantOwner"))
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    model.OwnerId = currentUser?.Id ?? "";
                }

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
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating the restaurant. Please try again.");
            }

            // Reload dropdown data
            if (User.IsInRole("RestaurantOwner"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                ViewData["OwnerId"] = new SelectList(new[] { currentUser }, "Id", "Email", currentUser?.Id);
            }
            else
            {
                var restaurantOwners = await _userManager.GetUsersInRoleAsync("RestaurantOwner");
                ViewData["OwnerId"] = new SelectList(restaurantOwners, "Id", "Email", model.OwnerId);
            }
            return View(model);
        }

        // GET: Restaurants/Edit/5
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null)
            {
                return NotFound();
            }

            // Check if RestaurantOwner can only edit their own restaurants
            if (User.IsInRole("RestaurantOwner"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (restaurant.OwnerId != currentUser?.Id)
                {
                    return Forbid();
                }
                ViewData["OwnerId"] = new SelectList(new[] { currentUser }, "Id", "Email", currentUser.Id);
            }
            else
            {
                var restaurantOwners = await _userManager.GetUsersInRoleAsync("RestaurantOwner");
                ViewData["OwnerId"] = new SelectList(restaurantOwners, "Id", "Email", restaurant.OwnerId);
            }

            var model = new RestaurantEditViewModel
            {
                Id = restaurant.Id,
                Title = restaurant.Title,
                Description = restaurant.Description,
                Address = restaurant.Address,
                Latitude = restaurant.Latitude,
                Longitude = restaurant.Longitude,
                CurrentImageUrl = restaurant.ImageUrl,
                OwnerId = restaurant.OwnerId,
                CreatedOn = restaurant.CreatedOn
            };

            return View(model);
        }

        // POST: Restaurants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> Edit(Guid id, RestaurantEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            try
            {
                // Check if RestaurantOwner can only edit their own restaurants
                if (User.IsInRole("RestaurantOwner"))
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (model.OwnerId != currentUser?.Id)
                    {
                        return Forbid();
                    }
                }

                // Validate image file if provided
                if (model.ImageFile != null && !_imageService.IsValidImage(model.ImageFile))
                {
                    ModelState.AddModelError(nameof(model.ImageFile), "Please upload a valid image file (JPG, PNG, GIF, WebP) under 2MB.");
                }

                if (ModelState.IsValid)
                {
                    var restaurant = await _context.Restaurants.FindAsync(id);
                    if (restaurant == null)
                    {
                        return NotFound();
                    }

                    // Update restaurant properties
                    restaurant.Title = model.Title;
                    restaurant.Description = model.Description;
                    restaurant.Address = model.Address;
                    restaurant.Latitude = model.Latitude;
                    restaurant.Longitude = model.Longitude;
                    restaurant.OwnerId = model.OwnerId;

                    // Handle image update
                    if (model.ImageFile != null)
                    {
                        // Delete old image if it's a local upload
                        if (!string.IsNullOrEmpty(restaurant.ImageUrl))
                        {
                            await _imageService.DeleteImageAsync(restaurant.ImageUrl);
                        }
                        
                        // Save new image
                        restaurant.ImageUrl = await _imageService.SaveImageAsync(model.ImageFile, Request);
                    }
                    else if (!string.IsNullOrEmpty(model.ImageUrl) && model.ImageUrl != model.CurrentImageUrl)
                    {
                        // Delete old image if switching to URL
                        if (!string.IsNullOrEmpty(restaurant.ImageUrl))
                        {
                            await _imageService.DeleteImageAsync(restaurant.ImageUrl);
                        }
                        
                        restaurant.ImageUrl = model.ImageUrl;
                    }

                    _context.Update(restaurant);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "Restaurant updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RestaurantsExists(model.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the restaurant. Please try again.");
            }

            // Reload dropdown data
            if (User.IsInRole("RestaurantOwner"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                ViewData["OwnerId"] = new SelectList(new[] { currentUser }, "Id", "Email", currentUser?.Id);
            }
            else
            {
                var restaurantOwners = await _userManager.GetUsersInRoleAsync("RestaurantOwner");
                ViewData["OwnerId"] = new SelectList(restaurantOwners, "Id", "Email", model.OwnerId);
            }
            
            return View(model);
        }

        // GET: Restaurants/Delete/5
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants
                .Include(r => r.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (restaurant == null)
            {
                return NotFound();
            }

            // Check if RestaurantOwner can only delete their own restaurants
            if (User.IsInRole("RestaurantOwner"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (restaurant.OwnerId != currentUser?.Id)
                {
                    return Forbid();
                }
            }

            return View(restaurant);
        }

        // POST: Restaurants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantOwner,Admin")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant != null)
            {
                // Check if RestaurantOwner can only delete their own restaurants
                if (User.IsInRole("RestaurantOwner"))
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (restaurant.OwnerId != currentUser?.Id)
                    {
                        return Forbid();
                    }
                }

                // Check if restaurant has reservations
                var hasReservations = await _context.Reservations.AnyAsync(r => r.RestaurantId == id);
                if (hasReservations)
                {
                    TempData["Error"] = "Cannot delete restaurant with existing reservations.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Restaurants.Remove(restaurant);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RestaurantsExists(Guid id)
        {
            return _context.Restaurants.Any(e => e.Id == id);
        }
    }
}
