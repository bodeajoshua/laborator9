namespace laborator1.Models
{
    public class Order                          
    {
        public int OrderID { get; set; } 

        public int? CustomerID { get; set; }
        public Customer? Customer { get; set; } = null!;

        public int? BookID { get; set; }
        public Book? Book { get; set; } = null!;       
        
        public DateTime OrderDate { get; set; }
    }
}
