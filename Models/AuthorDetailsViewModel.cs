namespace laborator1.Models
{
    public class AuthorDetailsViewModel
    {
        public int AuthorID { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<AuthorBookInfoViewModel> Books { get; set; } = new();
        public int TotalOrdersCount { get; set; }
        public decimal AveragePrice { get; set; }
    }
}   
