using System.ComponentModel.DataAnnotations;

namespace DgFarmerApi.Models;

public class Message
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    
    public int SenderId { get; set; }
    
    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Order Order { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
