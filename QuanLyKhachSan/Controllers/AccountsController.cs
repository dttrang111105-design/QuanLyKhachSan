
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Models;

[Authorize(Roles = "Admin")]
public class AccountsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: ACCOUNTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Accounts.ToListAsync());
    }

    // GET: ACCOUNTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var account = await _context.Accounts
            .FirstOrDefaultAsync(m => m.Id == id);
        if (account == null)
        {
            return NotFound();
        }

        return View(account);
    }

    // GET: ACCOUNTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ACCOUNTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Username,PasswordHash,Email,PhoneNumber,Role")] Account account)
    {
        if (ModelState.IsValid)
        {
            account.IsActive = true;
            _context.Add(account);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(account);
    }

    // GET: ACCOUNTS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var account = await _context.Accounts.FindAsync(id);
        if (account == null)
        {
            return NotFound();
        }
        return View(account);
    }

    // POST: ACCOUNTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Username,PasswordHash,Email,PhoneNumber,Role,IsActive,LastLogin,Customer,Employee,Id,CreatedAt,UpdatedAt,IsDeleted")] Account account)
    {
        if (id != account.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(account);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AccountExists(account.Id))
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
        return View(account);
    }

    // GET: ACCOUNTS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var account = await _context.Accounts
            .FirstOrDefaultAsync(m => m.Id == id);
        if (account == null)
        {
            return NotFound();
        }

        return View(account);
    }

    // POST: ACCOUNTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account != null)
        {
            _context.Accounts.Remove(account);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AccountExists(int? id)
    {
        return _context.Accounts.Any(e => e.Id == id);
    }
}
