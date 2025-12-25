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
        var orders = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.OrderItems.Any(oi => oi.Product.FarmerId == farmerId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var response = new List<OrderResponseDto>();

        foreach (var o in orders)
        {
            // Filter to only items belonging to this farmer
            var farmerItems = o.OrderItems
                .Where(oi => oi.Product != null && oi.Product.FarmerId == farmerId)
                .ToList();

            if (farmerItems.Count == 0) continue;

            // Calculate subtotal for this farmer's items only
            var farmerSubtotal = farmerItems.Sum(oi => oi.Price * oi.Quantity);

            response.Add(new OrderResponseDto
            {
                Id = o.Id,
                Status = o.Status,
                Total = farmerSubtotal, // Only this farmer's portion
                ShippingAddress = o.ShippingAddress,
                CustomerNote = o.CustomerNote,
                CreatedAt = o.CreatedAt,
                Items = farmerItems.Select(oi => new OrderItemDetailDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            });
        }

        return Ok(response);
    }
    
    // POST: api/orders (Buyers only - farmers cannot place orders)
    // Creates separate orders for each farmer's products
    [HttpPost]
    public async Task<ActionResult<List<OrderResponseDto>>> CreateOrder(CreateOrderDto dto)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null) return Unauthorized();
        
        // Check if user is a farmer - farmers cannot place orders
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();
        if (user.Role.ToLower() == "farmer")
        {
            return BadRequest(new { message = "Farmers cannot place orders. Please use a buyer account." });
        }
        
        // Get products and validate
        var productIds = dto.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products
            .Include(p => p.Farmer)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
            
        // Validate stock before creating orders
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
        }
        
        // Group items by farmer
        var itemsByFarmer = dto.Items
            .GroupBy(item => products[item.ProductId].FarmerId)
            .ToList();
        
        var createdOrders = new List<OrderResponseDto>();
        
        // Create separate order for each farmer
        foreach (var farmerGroup in itemsByFarmer)
        {
            var farmerOrderItems = new List<OrderItem>();
            decimal farmerTotal = 0;
            
            foreach (var item in farmerGroup)
            {
                var product = products[item.ProductId];
                var itemTotal = product.Price * item.Quantity;
                farmerTotal += itemTotal;
                
                farmerOrderItems.Add(new OrderItem
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
                Total = farmerTotal,
                ShippingAddress = dto.ShippingAddress,
                Phone = dto.Phone,
                CustomerNote = dto.Note,
                OrderItems = farmerOrderItems
            };
            
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            
            createdOrders.Add(new OrderResponseDto
            {
                Id = order.Id,
                Status = order.Status,
                Total = order.Total,
                ShippingAddress = order.ShippingAddress,
                CreatedAt = order.CreatedAt,
                Items = farmerOrderItems.Select(oi => new OrderItemDetailDto
                {
                    ProductId = oi.ProductId,
                    ProductName = products[oi.ProductId].Name,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            });
        }
        
        return Ok(createdOrders);
    }
    
    // POST: api/orders/5/accept (Farmer only - accept order)
    [HttpPost("{id}/accept")]
    [Authorize(Roles = "farmer")]
    public async Task<IActionResult> AcceptOrder(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        
        order.Status = "accepted";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Order accepted" });
    }
    
    // POST: api/orders/5/reject (Farmer only - reject order)
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "farmer")]
    public async Task<IActionResult> RejectOrder(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        
        order.Status = "rejected";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Order rejected" });
    }
    
    // POST: api/orders/5/cancel (Consumer only - cancel their own pending order)
    [HttpPost("{id}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null) return Unauthorized();
        
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        
        // Only order owner can cancel
        if (order.UserId != userId)
            return Forbid();
        
        // Can only cancel pending orders
        if (order.Status != "pending")
            return BadRequest(new { message = "Only pending orders can be cancelled" });
        
        order.Status = "cancelled";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Order cancelled" });
    }
}
