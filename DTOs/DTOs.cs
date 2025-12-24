using System.ComponentModel.DataAnnotations;

namespace DgFarmerApi.DTOs;

// Auth DTOs
public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    public string Role { get; set; } = "buyer";
    
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class AuthResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Avatar { get; set; }
}

// Product DTOs
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal Rating { get; set; }
    public string FarmerName { get; set; } = string.Empty;
    public int FarmerId { get; set; }
}

public class CreateProductDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [Range(0.01, 1000000)]
    public decimal Price { get; set; }
    
    [Required]
    public string Category { get; set; } = string.Empty;
    
    public string? Image { get; set; }
    
    [Required]
    public string Unit { get; set; } = "kg";
    
    [Range(0, 100000)]
    public int Stock { get; set; } = 0;
}

// Order DTOs
public class CreateOrderDto
{
    [Required]
    public List<OrderItemDto> Items { get; set; } = new();
    
    public string? ShippingAddress { get; set; }
    public string? Phone { get; set; }
    public string? Note { get; set; }
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class OrderResponseDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? ShippingAddress { get; set; }
    public string? CustomerNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDetailDto> Items { get; set; } = new();
}

public class OrderItemDetailDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
