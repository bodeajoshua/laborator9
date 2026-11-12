using Microsoft.ML.Data;

namespace laborator1.Models
{
    public class BookText
    {
        [LoadColumn(0)] public string Description { get; set; } = "";
        [LoadColumn(1)] public string Genre { get; set; } = "";
    }
}
