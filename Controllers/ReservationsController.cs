using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using proekt_za_6ca.Data;
using proekt_za_6ca.Data.Entities;
using proekt_za_6ca.Data.Enums;

namespace proekt_za_6ca.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ReservationsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            IQueryable<Reservation> reservations = _context.Reservations
                .Include(r => r.Owner)
                .Include(r => r.Restaurants);

            if (User.IsInRole("Admin"))
            {
                // Admins see all reservations
                ViewBag.Title = "All Reservations";
            }
            else if (User.IsInRole("RestaurantOwner"))
            {
                // Restaurant owners see reservations for their restaurants
                reservations = reservations.Where(r => r.Restaurants!.OwnerId == currentUser.Id);
                ViewBag.Title = "My Restaurant Reservations";
            }
            else
            {
                // Regular users see only their own reservations
                reservations = reservations.Where(r => r.OwnerId == currentUser.Id);
                ViewBag.Title = "My Reservations";
            }

            return View(await reservations.OrderByDescending(r => r.ReservationTime).ToListAsync());
        }

        // GET: Reservations for Restaurant Owner Dashboard
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> ManageReservations()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var reservations = await _context.Reservations
                .Include(r => r.Owner)
                .Include(r => r.Restaurants)
                .Where(r => r.Restaurants!.OwnerId == currentUser.Id)
                .OrderBy(r => r.Status)
                .ThenBy(r => r.ReservationTime)
                .ToListAsync();

            ViewBag.PendingCount = reservations.Count(r => r.Status == ReservationStatus.Pending);
            ViewBag.ConfirmedCount = reservations.Count(r => r.Status == ReservationStatus.Confirmed);
            ViewBag.DeniedCount = reservations.Count(r => r.Status == ReservationStatus.Denied);

            return View(reservations);
        }

        // POST: Approve Reservation
        [HttpPost]
        [Authorize(Roles = "RestaurantOwner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReservation(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    TempData["Error"] = "Invalid reservation ID.";
                    return RedirectToAction(nameof(ManageReservations));
                }

                var reservation = await _context.Reservations
                    .Include(r => r.Restaurants)
                    .Include(r => r.Owner)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                {
                    TempData["Error"] = "Reservation not found.";
                    return RedirectToAction(nameof(ManageReservations));
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (reservation.Restaurants?.OwnerId != currentUser?.Id)
                {
                    TempData["Error"] = "You can only approve reservations for your own restaurants.";
                    return RedirectToAction(nameof(ManageReservations));
                }

                if (reservation.Status != ReservationStatus.Pending)
                {
                    TempData["Error"] = "Only pending reservations can be approved.";
                    return RedirectToAction(nameof(ManageReservations));
                }

                // Check if reservation time is still in the future
                if (reservation.ReservationTime <= DateTime.Now)
                {
                    TempData["Error"] = "Cannot approve reservations for past dates.";
                    return RedirectToAction(nameof(ManageReservations));
                }

                // Check for conflicting reservations (optional business rule)
                var conflictingReservations = await _context.Reservations.CountAsync(r =>
                    r.RestaurantId == reservation.RestaurantId &&
                    r.Id != reservation.Id &&
                    r.Status == ReservationStatus.Confirmed &&
                    Math.Abs((r.ReservationTime - reservation.ReservationTime).TotalMinutes) < 30);

                if (conflictingReservations >= 5) // Example: max 5 reservations within 30 minutes
                {
                    TempData["Error"] = "Cannot approve reservation. Restaurant is fully booked at this time slot.";
                    return RedirectToAction(nameof(ManageReservations));
                }

                reservation.Status = ReservationStatus.Confirmed;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Reservation for {reservation.Owner?.FirstName} {reservation.Owner?.LastName} on {reservation.ReservationTime:MMM dd, yyyy} has been approved.";
                return RedirectToAction(nameof(ManageReservations));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while approving the reservation. Please try again.";
                return RedirectToAction(nameof(ManageReservations));
            }
        }

        // POST: Reject Reservation
        [HttpPost]
        [Authorize(Roles = "RestaurantOwner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReservation(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    TempData["Error"] = "Invalid reservation ID.";
                    return RedirectToAction(nameof(ManageReservations));
                }

                var reservation = await _context.Reservations
                    .Include(r => r.Restaurants)
                    .Include(r => r.Owner)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                {
                    TempData["Error"] = "Reservation not found.";
                    return RedirectToAction(nameof(ManageReservations));
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (reservation.Restaurants?.OwnerId != currentUser?.Id)
                {
                    TempData["Error"] = "You can only reject reservations for your own restaurants.";
                    return RedirectToAction(nameof(ManageReservations));
                }

                if (reservation.Status != ReservationStatus.Pending)
                {
                    TempData["Error"] = "Only pending reservations can be rejected.";
                    return RedirectToAction(nameof(ManageReservations));
                }

                reservation.Status = ReservationStatus.Denied;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Reservation for {reservation.Owner?.FirstName} {reservation.Owner?.LastName} on {reservation.ReservationTime:MMM dd, yyyy} has been rejected.";
                return RedirectToAction(nameof(ManageReservations));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while rejecting the reservation. Please try again.";
                return RedirectToAction(nameof(ManageReservations));
            }
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Owner)
                .Include(r => r.Restaurants)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (reservation == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            
            // Check access permissions
            if (!User.IsInRole("Admin"))
            {
                if (User.IsInRole("RestaurantOwner"))
                {
                    // Restaurant owners can only see reservations for their restaurants
                    if (reservation.Restaurants?.OwnerId != currentUser?.Id)
                        return Forbid();
                }
                else
                {
                    // Regular users can only see their own reservations
                    if (reservation.OwnerId != currentUser?.Id)
                        return Forbid();
                }
            }

            return View(reservation);
        }

        // GET: Reservations/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewData["RestaurantId"] = new SelectList(_context.Restaurants, "Id", "Title");
            
            var model = new Reservation
            {
                OwnerId = currentUser?.Id ?? "",
                ReservationTime = DateTime.Now.AddDays(1),
                PeopleCount = 2
            };
            
            return View(model);
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReservationTime,PeopleCount,RestaurantId,Comment")] Reservation reservation)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            // Set the owner to current user and default values
            reservation.Id = Guid.NewGuid();
            reservation.OwnerId = currentUser.Id;
            reservation.Status = ReservationStatus.Pending;
            reservation.CreatedOn = DateTime.Now;

            // Validate reservation time is in the future
            if (reservation.ReservationTime <= DateTime.Now)
            {
                ModelState.AddModelError("ReservationTime", "Reservation time must be in the future.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(reservation);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Your reservation has been submitted and is pending approval.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["RestaurantId"] = new SelectList(_context.Restaurants, "Id", "Title", reservation.RestaurantId);
            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Restaurants)
                .FirstOrDefaultAsync(r => r.Id == id);
            
            if (reservation == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            
            // Check permissions - only reservation owner or admin can edit, and only if pending
            if (!User.IsInRole("Admin") && reservation.OwnerId != currentUser?.Id)
            {
                return Forbid();
            }

            // Only allow editing if reservation is pending
            if (reservation.Status != ReservationStatus.Pending)
            {
                TempData["Error"] = "You can only edit pending reservations.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["RestaurantId"] = new SelectList(_context.Restaurants, "Id", "Title", reservation.RestaurantId);
            return View(reservation);
        }

        // POST: Reservations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,ReservationTime,PeopleCount,RestaurantId,Comment,Status,OwnerId,CreatedOn")] Reservation reservation)
        {
            if (id != reservation.Id) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var existingReservation = await _context.Reservations.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            
            if (existingReservation == null) return NotFound();

            // Check permissions
            if (!User.IsInRole("Admin") && existingReservation.OwnerId != currentUser?.Id)
            {
                return Forbid();
            }

            // Only allow editing if reservation is pending (unless admin)
            if (!User.IsInRole("Admin") && existingReservation.Status != ReservationStatus.Pending)
            {
                TempData["Error"] = "You can only edit pending reservations.";
                return RedirectToAction(nameof(Index));
            }

            // Validate reservation time is in the future
            if (reservation.ReservationTime <= DateTime.Now)
            {
                ModelState.AddModelError("ReservationTime", "Reservation time must be in the future.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["RestaurantId"] = new SelectList(_context.Restaurants, "Id", "Title", reservation.RestaurantId);
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Owner)
                .Include(r => r.Restaurants)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (reservation == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            
            // Check permissions - only reservation owner or admin can delete
            if (!User.IsInRole("Admin") && reservation.OwnerId != currentUser?.Id)
            {
                return Forbid();
            }

            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                // Check permissions
                if (!User.IsInRole("Admin") && reservation.OwnerId != currentUser?.Id)
                {
                    return Forbid();
                }

                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(Guid id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}
