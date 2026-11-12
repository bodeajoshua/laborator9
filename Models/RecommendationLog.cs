namespace laborator1.Models
{
    public class RecommendationLog
    {
        public int Id { get; set; }
        public int ReaderId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string BookIds { get; set; } = "";
    }
}
