namespace Testing.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Pending";
    public ICollection<OrderLine> OrderLines { get; set; } = [];
}
