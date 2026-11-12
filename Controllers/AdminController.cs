using laborator1.Models;
using laborator1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

public class AdminController(LibraryContext db, RecommenderTrainer trainer) : Controller
{

    public async Task<IActionResult> Dashboard()
    {
        ViewData["TotalRatings"] = await db.Rating.CountAsync();
        ViewData["DistinctReaders"] = await db.Rating.Select(r => r.ReaderId).Distinct().CountAsync();
        ViewData["AverageRating"] = db.Rating.Any()
            ? await db.Rating.AverageAsync(r => (double)r.Stars)
            : 0;

        var ratingDistribution = await db.Rating
            .GroupBy(r => r.Stars)
            .OrderBy(g => g.Key)
            .Select(g => new { Stars = g.Key, Count = g.Count() })
            .ToListAsync();

        ViewData["RatingStars"] = System.Text.Json.JsonSerializer.Serialize(ratingDistribution.Select(d => $"{d.Stars} Stele"));
        ViewData["RatingCounts"] = System.Text.Json.JsonSerializer.Serialize(ratingDistribution.Select(d => d.Count));

        var avgRatingPerGenre = await db.Rating
            .Include(r => r.Book)
            .ThenInclude(b => b.Genre)
            .Where(r => r.Book != null && r.Book.Genre != null)
            .GroupBy(r => r.Book.Genre.Name) 
            .Select(g => new { Genre = g.Key, AvgScore = g.Average(r => r.Stars) })
            .ToListAsync();

        ViewData["GenreLabels"] = System.Text.Json.JsonSerializer.Serialize(avgRatingPerGenre.Select(g => g.Genre));
        ViewData["GenreAverages"] = System.Text.Json.JsonSerializer.Serialize(avgRatingPerGenre.Select(g => Math.Round(g.AvgScore, 2)));

        return View();
    }

    public async Task<IActionResult> Log(
    int? readerFilter,
    DateTime? startDate,
    DateTime? endDate,
    string? sortOrder,
    int? pageNumber)
    {
        ViewData["CurrentReaderFilter"] = readerFilter;
        ViewData["StartDateFilter"] = startDate?.ToString("yyyy-MM-dd");
        ViewData["EndDateFilter"] = endDate?.ToString("yyyy-MM-dd");
        ViewData["DateSortParm"] = string.IsNullOrEmpty(sortOrder) ? "date_asc" : "";

        var logsQuery = db.RecommendationLog.AsQueryable();

        if (readerFilter.HasValue)
        {
            logsQuery = logsQuery.Where(l => l.ReaderId == readerFilter.Value);
        }

        if (startDate.HasValue)
        {
            logsQuery = logsQuery.Where(l => l.Date >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            var endDateTime = endDate.Value.AddDays(1).AddTicks(-1);
            logsQuery = logsQuery.Where(l => l.Date <= endDateTime);
        }

        logsQuery = sortOrder switch
        {
            "date_asc" => logsQuery.OrderBy(l => l.Date),
            _ => logsQuery.OrderByDescending(l => l.Date),
        };

        int pageSize = 9;
        return View(await PaginatedList<RecommendationLog>.CreateAsync(logsQuery.AsNoTracking(), pageNumber ?? 1, pageSize));
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Ratings = await db.Rating.CountAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportRatings(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Msg"] = "Alegeți un fișier CSV.";
            return RedirectToAction(nameof(Index));
        }

        var ratings = new List<Rating>();
        using var reader = new StreamReader(file.OpenReadStream());
        await reader.ReadLineAsync(); // sărim antetul
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            var p = line.Split(',');
            if (p.Length < 3 || !int.TryParse(p[0], out var u) ||
                int.TryParse(p[1], out var b) || !int.TryParse(p[2], out var s))
                continue;

            ratings.Add(new Rating { ReaderId = u, BookID = b, Stars = s, CreatedAt = DateTime.UtcNow });
        }

        db.Rating.AddRange(ratings);
        await db.SaveChangesAsync();
        TempData["Msg"] = $"Au fost importate {ratings.Count} evaluări.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Retrain()
    {
        var (rmse, count) = await trainer.TrainAsync();
        TempData["Msg"] = $"Model reantrenat pe {count} evaluări. RMSE = {rmse:0.00}";
        return RedirectToAction(nameof(Index));
    }
}