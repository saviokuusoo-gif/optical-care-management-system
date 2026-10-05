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

        // Gender Filter
        if (!string.IsNullOrWhiteSpace(gender))
        {
            query = query.Where(f => f.Gender.ToLower() == gender.ToLower());
        }

        // Shape Filter
        if (!string.IsNullOrWhiteSpace(shape))
        {
            query = query.Where(f => f.Shape.ToLower() == shape.ToLower());
        }

        // Material Filter
        if (!string.IsNullOrWhiteSpace(material))
        {
            query = query.Where(f => f.Material.ToLower() == material.ToLower());
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

        // Fetch candidates for smart search and ranking
        var allFilteredFrames = await query.ToListAsync();
        List<Frame> resultFrames;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var rawSearch = search.Trim();
            var normalizedSearch = NormalizeSearchString(rawSearch);

            var searchTokens = normalizedSearch
                .Split(new[] { ' ', ',', '-', '+' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant())
                .Distinct()
                .ToList();

            var rawTokens = rawSearch
                .Split(new[] { ' ', ',', '-', '+' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant())
                .Distinct()
                .ToList();

            var allTokens = searchTokens.Union(rawTokens).Distinct().ToList();

            string GetFrameSearchableText(Frame f) =>
                $"{f.Name} {f.Brand} {f.Code} {f.Gender} {f.Material} {f.Shape} {f.Color}".ToLowerInvariant();

            // 1. Direct exact phrase match across combined attributes
            var exactPhraseMatches = allFilteredFrames
                .Where(f => GetFrameSearchableText(f).Contains(rawSearch.ToLowerInvariant()) ||
                            GetFrameSearchableText(f).Contains(normalizedSearch.ToLowerInvariant()))
                .ToList();

            if (exactPhraseMatches.Count > 0)
            {
                resultFrames = exactPhraseMatches;
            }
            else
            {
                // 2. Multi-term AND match: each token matches somewhere in frame properties (or fuzzy match)
                var allTokenMatches = allFilteredFrames
                    .Where(f =>
                    {
                        var text = GetFrameSearchableText(f);
                        return searchTokens.All(tok => MatchesToken(text, tok));
                    })
                    .ToList();

                if (allTokenMatches.Count > 0)
                {
                    resultFrames = allTokenMatches;
                }
                else
                {
                    // 3. Multi-term OR match (ranked by match relevance)
                    resultFrames = allFilteredFrames
                        .Select(f =>
                        {
                            var text = GetFrameSearchableText(f);
                            int score = 0;
                            foreach (var tok in allTokens)
                            {
                                if (text.Contains(tok)) score += 3;
                                else if (MatchesToken(text, tok)) score += 2;
                            }
                            return new { Frame = f, Score = score };
                        })
                        .Where(x => x.Score > 0)
                        .OrderByDescending(x => x.Score)
                        .Select(x => x.Frame)
                        .ToList();
                }
            }
        }
        else
        {
            resultFrames = allFilteredFrames;
        }

        // Sorting
        resultFrames = sort switch
        {
            "price_low" => resultFrames.OrderBy(f => f.Price).ToList(),
            "price_high" => resultFrames.OrderByDescending(f => f.Price).ToList(),
            "newest" => resultFrames.OrderByDescending(f => f.Id).ToList(),
            _ => resultFrames.OrderBy(f => f.Name).ToList()
        };

        // Keep filter values
        ViewBag.Search = search;
        ViewBag.Gender = gender;
        ViewBag.Shape = shape;
        ViewBag.Material = material;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Sort = sort;

        return View(resultFrames);
    }

    private static string NormalizeSearchString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var text = input.ToLowerInvariant();

        var replacements = new Dictionary<string, string>
        {
            { "aye", "eye" },
            { "ayes", "eyes" },
            { "elagent", "elegant" },
            { "elegent", "elegant" },
            { "cataye", "cat eye" },
            { "cateye", "cat eye" },
            { "avitor", "aviator" },
            { "aveator", "aviator" },
            { "geomatric", "geometric" },
            { "geomtric", "geometric" },
            { "titaium", "titanium" },
            { "titaniuam", "titanium" },
            { "actate", "acetate" },
            { "acetat", "acetate" },
            { "glases", "glasses" },
            { "glass", "glasses" },
            { "sunglass", "sunglasses" },
            { "sun glass", "sunglasses" }
        };

        foreach (var kvp in replacements)
        {
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                $@"\b{System.Text.RegularExpressions.Regex.Escape(kvp.Key)}\b",
                kvp.Value,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return text;
    }

    private static bool MatchesToken(string searchableText, string token)
    {
        if (searchableText.Contains(token)) return true;

        var words = searchableText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (token.Length >= 4)
        {
            foreach (var word in words)
            {
                if (word.StartsWith(token) || token.StartsWith(word)) return true;
                if (LevenshteinDistance(word, token) <= (token.Length > 5 ? 2 : 1)) return true;
            }
        }
        else if (token.Length >= 3)
        {
            foreach (var word in words)
            {
                if (word.StartsWith(token) || LevenshteinDistance(word, token) <= 1) return true;
            }
        }

        return false;
    }

    private static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        var d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
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
            BookingDate = DateTime.UtcNow,
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