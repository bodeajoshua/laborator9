
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using laborator1.Models;

public class AuthorsController : Controller
{
    private readonly LibraryContext _context;

    public AuthorsController(LibraryContext context)
    {
        _context = context;
    }

    // GET: AUTHORS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Author.ToListAsync());
    }

    // GET: AUTHORS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var author = await _context.Author
            .FirstOrDefaultAsync(m => m.ID == id);
        if (author == null)
        {
            return NotFound();
        }

        var booksInfo = await _context.Book
            .Where(b => b.AuthorID == id)
            .Include(b => b.Genre)
            .Select(b => new AuthorBookInfoViewModel
            {
                Title = b.Title,
                GenreName = b.Genre != null ? b.Genre.Name : "N/A",
                Price = b.Price,
                OrderCount = _context.Order.Count(o => o.BookID == b.ID)
            })
            .ToListAsync();

        decimal averagePrice = 0;
        if (await _context.Book.AnyAsync(b => b.AuthorID == id))
        {
            averagePrice = await _context.Book
                .Where(b => b.AuthorID == id)
                .AverageAsync(b => b.Price);
        }

        var viewModel = new AuthorDetailsViewModel
        {
            AuthorID = author.ID,
            Name = author.FullName,
            Books = booksInfo,
            TotalOrdersCount = booksInfo.Sum(b => b.OrderCount),
            AveragePrice = averagePrice
        };


        return View(viewModel);
    }

    // GET: AUTHORS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: AUTHORS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,FirstName,LastName,FullName,Books")] Author author)
    {
        if (ModelState.IsValid)
        {
            _context.Add(author);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(author);
    }

    // GET: AUTHORS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var author = await _context.Author.FindAsync(id);
        if (author == null)
        {
            return NotFound();
        }
        return View(author);
    }

    // POST: AUTHORS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,FirstName,LastName,FullName,Books")] Author author)
    {
        if (id != author.ID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(author);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(author.ID))
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
        return View(author);
    }

    // GET: AUTHORS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var author = await _context.Author
            .FirstOrDefaultAsync(m => m.ID == id);
        if (author == null)
        {
            return NotFound();
        }

        return View(author);
    }

    // POST: AUTHORS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var author = await _context.Author.FindAsync(id);
        if (author != null)
        {
            _context.Author.Remove(author);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AuthorExists(int? id)
    {
        return _context.Author.Any(e => e.ID == id);
    }
}
