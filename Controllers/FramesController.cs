using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using optical_care_management_system.Models;

namespace optical_care_management_system.Controllers;

[Authorize]
public class FramesController : Controller
{
    private readonly ApplicationDbContext _context;

    public FramesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Frames
    public async Task<IActionResult> Index(
        string? search,
        string? gender,
        string? shape,
        string? material,
        decimal? minPrice,
        decimal? maxPrice,
        string? sort)
    {
        var query = _context.Frames.AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f =>
                f.Name.Contains(search) ||
                f.Brand.Contains(search));
        }

        // Gender Filter
        if (!string.IsNullOrWhiteSpace(gender))
        {
            query = query.Where(f => f.Gender == gender);
        }

        // Shape Filter
        if (!string.IsNullOrWhiteSpace(shape))
        {
            query = query.Where(f => f.Shape == shape);
        }

        // Material Filter
        if (!string.IsNullOrWhiteSpace(material))
        {
            query = query.Where(f => f.Material == material);
        }

        // Price Filter
        if (minPrice.HasValue)
        {
            query = query.Where(f => f.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(f => f.Price <= maxPrice.Value);
        }

        // Sorting
        query = sort switch
        {
            "price_low" => query.OrderBy(f => f.Price),

            "price_high" => query.OrderByDescending(f => f.Price),

            "newest" => query.OrderByDescending(f => f.Id),

            _ => query.OrderBy(f => f.Name)
        };

        // Keep filter values
        ViewBag.Search = search;
        ViewBag.Gender = gender;
        ViewBag.Shape = shape;
        ViewBag.Material = material;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Sort = sort;

        var frames = await query.ToListAsync();

        return View(frames);
    }


    // GET: Frames/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var frame = await _context.Frames
            .FirstOrDefaultAsync(f => f.Id == id);

        if (frame == null)
        {
            return NotFound();
        }

        return View(frame);
    }

    // GET: Frames/BookFrame/5
    [HttpGet]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> BookFrame(int id)
    {
        var frame = await _context.Frames.FirstOrDefaultAsync(f => f.Id == id);

        if (frame == null)
        {
            return NotFound();
        }

        if (!frame.IsAvailable)
        {
            TempData["Error"] = $"{frame.Name} is currently sold out.";
            return RedirectToAction(nameof(Index));
        }

        return View(frame);
    }


    // POST: Frames/BookFrame/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> BookFrame(int id, string? notes)
    {
        var frame = await _context.Frames.FirstOrDefaultAsync(f => f.Id == id);

        if (frame == null)
        {
            return NotFound();
        }

        if (!frame.IsAvailable)
        {
            TempData["Error"] = $"{frame.Name} is currently sold out.";
            return RedirectToAction(nameof(Index));
        }

        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            return RedirectToAction("Logout", "Account");
        }

        // One open booking per frame per patient, so a double submit does not
        // create duplicates.
        var alreadyBooked = await _context.FrameBookings
            .AnyAsync(b => b.PatientId == patient.Id
                           && b.FrameId == frame.Id
                           && b.Status == "Pending");

        if (alreadyBooked)
        {
            TempData["Error"] = $"You already have a pending booking for {frame.Name}.";
            return RedirectToAction(nameof(MyBookings));
        }

        _context.FrameBookings.Add(new FrameBooking
        {
            PatientId = patient.Id,
            FrameId = frame.Id,
            BookingDate = DateTime.Now,
            Status = "Pending",
            Notes = notes ?? string.Empty
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"{frame.Name} booked. The clinic will confirm shortly.";

        return RedirectToAction(nameof(MyBookings));
    }


    // GET: Frames/MyBookings
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> MyBookings()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            return RedirectToAction("Logout", "Account");
        }

        var bookings = await _context.FrameBookings
            .Include(b => b.Frame)
            .Where(b => b.PatientId == patient.Id)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return View(bookings);
    }


    // POST: Frames/CancelBooking/5 - a patient withdrawing their own request
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var patient = await GetCurrentPatientAsync();

        if (patient is null)
        {
            return RedirectToAction("Logout", "Account");
        }

        var booking = await _context.FrameBookings
            .FirstOrDefaultAsync(b => b.Id == id && b.PatientId == patient.Id);

        if (booking is null)
        {
            return NotFound();
        }

        booking.Status = "Canceled";
        await _context.SaveChangesAsync();

        TempData["Success"] = "Booking canceled.";

        return RedirectToAction(nameof(MyBookings));
    }


    private async Task<Patient?> GetCurrentPatientAsync()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return await _context.Patients.FirstOrDefaultAsync(p => p.UserAccountId == userId);
    }
    // GET: Frames/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }


    // POST: Frames/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Frame frame)
    {
        if (ModelState.IsValid)
        {
            _context.Frames.Add(frame);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Frame created successfully.";

            return RedirectToAction(nameof(Index));
        }

        return View(frame);
    }


    // GET: Frames/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var frame = await _context.Frames.FindAsync(id);

        if (frame == null)
        {
            return NotFound();
        }

        return View(frame);
    }


    // POST: Frames/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, Frame frame)
    {
        if (id != frame.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(frame);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Frame updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Frames.Any(f => f.Id == frame.Id))
                {
                    return NotFound();
                }

                throw;
            }
        }

        return View(frame);
    }


    // GET: Frames/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var frame = await _context.Frames
            .FirstOrDefaultAsync(f => f.Id == id);

        if (frame == null)
        {
            return NotFound();
        }

        return View(frame);
    }


    // POST: Frames/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var frame = await _context.Frames.FindAsync(id);

        if (frame != null)
        {
            _context.Frames.Remove(frame);

            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Frame deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
}