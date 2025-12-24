using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DgFarmerApi.Models;

public class Product
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
    
    public string? Image { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = "kg";
    
    public int Stock { get; set; } = 0;
    
    [Column(TypeName = "decimal(2,1)")]
    public decimal Rating { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign key
    public int FarmerId { get; set; }
    
    // Navigation property
    public User Farmer { get; set; } = null!;
}
