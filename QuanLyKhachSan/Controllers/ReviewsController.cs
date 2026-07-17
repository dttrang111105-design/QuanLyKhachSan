
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Models;

[Authorize(Roles = "Customer")]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: REVIEWS
    public async Task<IActionResult> Index()
    {
        var data = _context.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Room);

        return View(await data.ToListAsync());
    }

    // GET: REVIEWS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var review = await _context.Reviews
            .FirstOrDefaultAsync(m => m.Id == id);
        if (review == null)
        {
            return NotFound();
        }

        return View(review);
    }

    // GET: REVIEWS/Create
    public IActionResult Create()
    {
        ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName");

        ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "RoomNumber");

        return View();
    }

    // POST: REVIEWS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    [Bind("CustomerId,RoomId,Rating,Comment")]
    Review review)
    {
        if (!ModelState.IsValid)
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers,"Id","FullName",review.CustomerId);

            ViewData["RoomId"] = new SelectList(_context.Rooms,"Id","RoomNumber",review.RoomId);

            return View(review);
        }

        _context.Reviews.Add(review);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: REVIEWS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound();
        }
        return View(review);
    }

    // POST: REVIEWS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("CustomerId,Customer,RoomId,Room,Rating,Comment,Id,CreatedAt,UpdatedAt,IsDeleted")] Review review)
    {
        if (id != review.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(review);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReviewExists(review.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(review);
    }

    // GET: REVIEWS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var review = await _context.Reviews
            .FirstOrDefaultAsync(m => m.Id == id);
        if (review == null)
        {
            return NotFound();
        }

        return View(review);
    }

    // POST: REVIEWS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review != null)
        {
            _context.Reviews.Remove(review);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ReviewExists(int? id)
    {
        return _context.Reviews.Any(e => e.Id == id);
    }
}
