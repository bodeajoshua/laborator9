namespace laborator1.Models
{
    public class AuthorBookInfoViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string? GenreName { get; set; }
        public decimal Price { get; set; }
        public int OrderCount { get; set; }
    }
}
