
using laborator1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;


public record SuggestGenreRequest(string Description);


public class BooksController : Controller
{
    private readonly LibraryContext _context;
    private readonly ILogger<BooksController> _logger;
    public BooksController(LibraryContext context, ILogger<BooksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: BOOKS
    public async Task<IActionResult> Index(string? sortOrder, string? currentFilter, string? searchString, int? authorId,  int? pageNumber)
    {
        ViewData["CurrentSort"] = sortOrder;
        ViewData["TitleSortParm"] = sortOrder == "title" ? "title_desc" : "title";
        ViewData["PriceSortParm"] = sortOrder == "price" ? "price_desc" : "price";
        ViewData["AuthorSortParm"] = sortOrder == "author" ? "author_desc" : "author";

        if (searchString != null)
        {
            pageNumber = 1;
        }
        else
        {
            searchString = currentFilter;
        }

        ViewData["CurrentFilter"] = searchString;
        ViewData["CurrentAuthor"] = authorId;

        var books = _context.Book
        .Include(b => b.Author)
        .Include(b => b.Genre)
        .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            books = books.Where(b => b.Title.Contains(searchString));
        }

        if (authorId.HasValue && authorId.Value > 0)
        {
            books = books.Where(b => b.AuthorID == authorId.Value);
        }

        ViewData["AuthorID"] = new SelectList(_context.Author.OrderBy(a => a.LastName), "ID", "FullName", authorId);

        books = sortOrder switch
        {
            "title" => books.OrderBy(b => b.Title),
            "title_desc" => books.OrderByDescending(b => b.Title),
            "price" => books.OrderBy(b => b.Price),
            "price_desc" => books.OrderByDescending(b => b.Price),
            "author" => books.OrderBy(b => b.Author.LastName),
            "author_desc" => books.OrderByDescending(b => b.Author.LastName),
            _ => books.OrderByDescending(b => b.Price)
        };

        var rows = books.Select(b => new BookListItem(
            b.ID,
            b.Title,
            b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : "",
            b.Genre != null ? b.Genre.Name : "",
            b.Price,
            b.Orders.Count
        ));

        const int pageSize = 9;

        return View(await PaginatedList<BookListItem>.CreateAsync(rows, pageNumber ?? 1, pageSize));
    }

    // GET: BOOKS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Book
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .Include(b => b.Orders).ThenInclude(o => o.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ID == id);

        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    // GET: BOOKS/Create
    public IActionResult Create()
    {
        ViewData["AuthorID"] = new SelectList(_context.Author.OrderBy(a => a.LastName), "ID", "FullName");
        ViewData["GenreID"] = new SelectList(_context.Genre.OrderBy(g => g.Name), "ID", "Name");
        return View();
    }

    // POST: BOOKS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Title,Price,GenreID,AuthorID")] Book book)
    {
        if (ModelState.IsValid)
        {
            _context.Add(book);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewData["AuthorID"] = new SelectList(_context.Author.OrderBy(a => a.LastName), "ID", "FullName");
        ViewData["GenreID"] = new SelectList(_context.Genre.OrderBy(g => g.Name), "ID", "Name");

        return View(book);
    }

    // GET: BOOKS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Book.FindAsync(id);
        if (book == null)
        {
            return NotFound();
        }

        // Mapăm entitatea din baza de date în ViewModel
        var vm = new BookEditViewModel
        {
            ID = book.ID,
            Title = book.Title,
            Price = book.Price,
            AuthorID = book.AuthorID,
            GenreID = book.GenreID
        };

        ViewData["AuthorID"] = new SelectList(_context.Author.OrderBy(a => a.LastName), "ID", "FullName");
        ViewData["GenreID"] = new SelectList(_context.Genre.OrderBy(g => g.Name), "ID", "Name");

        return View(vm);
    }

    // POST: BOOKS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ID,Title,Price,AuthorID,GenreID")] Book book)
    {
        if (id != book.ID) return NotFound();
        if (!ModelState.IsValid) return View(book);
        _context.Update(book);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }


    // GET: BOOKS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Book
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .FirstOrDefaultAsync(m => m.ID == id);

        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    // POST: BOOKS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var book = await _context.Book.FindAsync(id);
        if (book != null)
        {
            _context.Book.Remove(book);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult SuggestGenre([FromBody] SuggestGenreRequest req,
        [FromServices] PredictionEnginePool<BookText, GenrePrediction> pool)
    {
        if (string.IsNullOrWhiteSpace(req.Description))
            return BadRequest("Descrierea este goală.");

        var p = pool.Predict("GenreClassifier", new BookText { Description = req.Description });

        return Json(new { genre = p.Genre, confidence = p.Score.Max() });
    }

    private bool BookExists(int? id)
    {
        return _context.Book.Any(e => e.ID == id);
    }
}
