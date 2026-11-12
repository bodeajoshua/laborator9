using Biblioteca.Api;
using Biblioteca.Data;
using laborator1.Models;
using laborator1.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.ML;
using OllamaSharp;
using OpenAI;
using Scalar.AspNetCore;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("LibraryContext") ?? throw new InvalidOperationException("Connection string 'LibraryContext' not found.");

builder.Services.AddDbContext<LibraryContext>(options => options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services
   .AddPredictionEnginePool<BookRating, BookRatingPrediction>()
   .FromFile(modelName: "BookRecommender",
             filePath: "MLModels/BookRecommender.zip",
             watchForChanges: true);

builder.Services
   .AddPredictionEnginePool<BookText, GenrePrediction>()
   .FromFile(modelName: "GenreClassifier",
             filePath: "MLModels/GenreClassifier.zip",
             watchForChanges: true);

builder.Services.AddValidation();   // validare automată pentru Minimal API
builder.Services.AddOpenApi();

var endpoint = new Uri("http://localhost:11434/v1");
IChatClient chat = new OpenAIClient(new ApiKeyCredential("ollama"),
    new OpenAIClientOptions { Endpoint = endpoint })
    .GetChatClient("llama3.2")
    .AsIChatClient();
builder.Services.AddSingleton<IChatClient>(chat);
builder.Services.AddScoped<RecommenderTrainer>();

builder.Services.AddGrpc();
builder.Services.AddScoped<RecommendationService>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
    new OllamaApiClient(new Uri("http://localhost:11434"), "embeddinggemma"));    //misiunea 3: nomic-embed-text
builder.Services.AddSingleton<DocumentIndex>();



var app = builder.Build();





if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json (OpenAPI 3.1 în .NET 10)
    app.MapScalarApiReference();    // /scalar
}

app.MapMcp("/mcp");

app.MapGrpcService<RecommenderGrpcService>();

app.MapLibraryApi();


app.MapPost("/api/v2/books/{id:int}/pitch", async (int id, LibraryContext db, IChatClient ai) =>
{
    var b = await db.Book.Include(x => x.Author).Include(x => x.Genre)
                         .FirstOrDefaultAsync(x => x.ID == id);
    if (b is null) return Results.NotFound();

    List<ChatMessage> messages =
    [
        new(ChatRole.System, "Ești bibliotecar. Răspunde în română, în cel mult 2 propoziții."),
        new(ChatRole.User, $"Recomandă „{b.Title}” de {b.Author?.FullName} " +
                           $"({b.Genre?.Name}). Descriere: ")
    ];
    ChatResponse r = await ai.GetResponseAsync(messages);
    return Results.Ok(new { b.Title, Pitch = r.Text });
});

app.MapGet("/api/v2/ask", async (string q, IChatClient ai) =>
    (await ai.GetResponseAsync(q)).Text);

app.MapGet("/api/v2/readers/{id:int}/recommendations",
 async (int id, int? top, LibraryContext db,
 PredictionEnginePool<BookRating, BookRatingPrediction> pool) =>
 {
     var n = Math.Clamp(top ?? 5, 1, 20);
     var rated = await db.Rating.Where(r => r.ReaderId == id)
         .Select(r => r.BookID).ToListAsync();
     var candidates = await db.Book.AsNoTracking()
         .Where(b => !rated.Contains(b.ID))
         .Select(b => new { b.ID, b.Title }).ToListAsync();

     var recs = candidates
         .Select(b => new RecommendationDto(b.ID, b.Title,
             pool.Predict("BookRecommender",
                 new BookRating { UserId = id, BookId = b.ID }).Score, false))
         .Where(r => !float.IsNaN(r.Score))
         .OrderByDescending(r => r.Score)
         .Take(n).ToList();

     if (recs.Count == 0) // cold start: cele mai bine evaluate cărți (minimum 10 evaluări)
     {
         var popular = await db.Rating
             .GroupBy(r => r.BookID)
             .Where(g => g.Count() >= 15) // înlocuiește 10 cu pragul tău personal dacă difera
             .Select(g => new { BookID = g.Key, Avg = g.Average(r => r.Stars) })
             .OrderByDescending(x => x.Avg).Take(n)
             .ToListAsync();

         var ids = popular.Select(p => p.BookID).ToList();
         var titles = await db.Book.Where(b => ids.Contains(b.ID))
             .ToDictionaryAsync(b => b.ID, b => b.Title);

         recs = popular.Select(p => new RecommendationDto(
             p.BookID, titles[p.BookID], (float)p.Avg, true)).ToList();
     }

     var log = new RecommendationLog
     {
         ReaderId = id,
         Date = DateTime.Now,
         BookIds = string.Join(",", recs.Select(r => r.BookId))
     };
     db.RecommendationLog.Add(log);
     await db.SaveChangesAsync();

     return TypedResults.Ok(recs);
 })
.WithTags("Recomandări");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    db.Database.Migrate();          // aplică migrările (înlocuiește Update-Database)
    LibrarySeed.Seed(db);

    // ===== Construim indexul o singură dată, la pornire =====
    var index = scope.ServiceProvider.GetRequiredService<DocumentIndex>();
    await index.BuildAsync(db, Path.Combine(app.Environment.ContentRootPath, "Docs", "regulament.md"));
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "start",
    pattern: "start",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
