using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DgFarmerApi.Data;
using DgFarmerApi.Models;
using DgFarmerApi.DTOs;
using DgFarmerApi.Services;

namespace DgFarmerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthService _authService;
    
    public OrdersController(AppDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }
    
    // GET: api/orders
    [HttpGet]
    public async Task<ActionResult<List<OrderResponseDto>>> GetMyOrders()
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null) return Unauthorized();
        
        var orders = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderResponseDto
            {
                Id = o.Id,
                Status = o.Status,
                Total = o.Total,
                ShippingAddress = o.ShippingAddress,
                CreatedAt = o.CreatedAt,
                Items = o.OrderItems.Select(oi => new OrderItemDetailDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            }).ToListAsync();
            
        return Ok(orders);
    }
    
    // GET: api/orders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(int id)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null) return Unauthorized();
        
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            
        if (order == null) return NotFound();
        
        return Ok(new OrderResponseDto
        {
            Id = order.Id,
            Status = order.Status,
            Total = order.Total,
            ShippingAddress = order.ShippingAddress,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems.Select(oi => new OrderItemDetailDto
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product.Name,
                Quantity = oi.Quantity,
                Price = oi.Price
            }).ToList()
        });
    }
    
    // GET: api/orders/received (for Farmers)
    [HttpGet("received")]
    [Authorize(Roles = "farmer")]
    public async Task<ActionResult<List<OrderResponseDto>>> GetReceivedOrders()
    {
        var farmerId = _authService.GetUserIdFromToken(User);
        if (farmerId == null) return Unauthorized();

        // Get all orders that contain products from this farmer
        // Note: This is a simplified approach. In a real app, you might want to return only the items belonging to the farmer.
        // For this MVP, we will return the full order but strictly filtered.
        
        // This query fetches orders where at least one item belongs to the logged-in farmer
        var orders = await _db.Orders
            .Include(o => o.User) // Buyer details
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.OrderItems.Any(oi => oi.Product.FarmerId == farmerId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var response = orders.Select(o => new OrderResponseDto
        {
            Id = o.Id,
            Status = o.Status,
            Total = o.Total,
            ShippingAddress = o.ShippingAddress,
            CustomerNote = o.CustomerNote,
            CreatedAt = o.CreatedAt,
            // Only include items that belong to this farmer
            Items = o.OrderItems
                .Where(oi => oi.Product.FarmerId == farmerId)
                .Select(oi => new OrderItemDetailDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
        }).ToList();

        return Ok(response);
    }
    
    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder(CreateOrderDto dto)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null) return Unauthorized();
        
        // Get products and calculate total
        var productIds = dto.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
            
        decimal total = 0;
        var orderItems = new List<OrderItem>();
        
        foreach (var item in dto.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                return BadRequest(new { message = $"Product {item.ProductId} not found" });
            }
            
            if (product.Stock < item.Quantity)
            {
                return BadRequest(new { message = $"Not enough stock for {product.Name}" });
            }
            
            var itemTotal = product.Price * item.Quantity;
            total += itemTotal;
            
            orderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = product.Price
            });
            
            // Reduce stock
            product.Stock -= item.Quantity;
        }
        
        var order = new Order
        {
            UserId = userId.Value,
            Total = total,
            ShippingAddress = dto.ShippingAddress,
            Phone = dto.Phone,
            CustomerNote = dto.Note,
            OrderItems = orderItems
        };
        
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new OrderResponseDto
        {
            Id = order.Id,
            Status = order.Status,
            Total = order.Total,
            ShippingAddress = order.ShippingAddress,
            CreatedAt = order.CreatedAt,
            Items = orderItems.Select(oi => new OrderItemDetailDto
            {
                ProductId = oi.ProductId,
                ProductName = products[oi.ProductId].Name,
                Quantity = oi.Quantity,
                Price = oi.Price
            }).ToList()
        });
    }
    
    // PUT: api/orders/5/status (Farmer only - update order status)
    [HttpPut("{id}/status")]
    [Authorize(Roles = "farmer")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        
        var validStatuses = new[] { "pending", "processing", "delivered", "cancelled" };
        if (!validStatuses.Contains(status.ToLower()))
        {
            return BadRequest(new { message = "Invalid status" });
        }
        
        order.Status = status.ToLower();
        if (status.ToLower() == "delivered")
        {
            order.DeliveredAt = DateTime.UtcNow;
        }
        
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
