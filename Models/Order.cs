using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DgFarmerApi.Models;

public class Order
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    public string Status { get; set; } = "pending"; // pending, processing, delivered, cancelled
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }
    
    public string? ShippingAddress { get; set; }
    
    public string? Phone { get; set; }
    
    public string? CustomerNote { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? DeliveredAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    
    public int ProductId { get; set; }
    
    public int Quantity { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
    
    // Navigation properties
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
