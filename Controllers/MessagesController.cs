using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DgFarmerApi.Data;
using DgFarmerApi.DTOs;
using DgFarmerApi.Models;
using DgFarmerApi.Services;

namespace DgFarmerApi.Controllers;

[ApiController]
[Route("api/orders/{orderId}/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthService _authService;
    
    public MessagesController(AppDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }
    
    // GET: api/orders/5/messages
    [HttpGet]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(int orderId)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null) return Unauthorized();
        
        // Verify user has access to this order
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);
            
        if (order == null) return NotFound();
        
        // User must be the order owner OR a farmer with products in this order
        var user = await _db.Users.FindAsync(userId);
        var isBuyer = order.UserId == userId;
        var isFarmer = user?.Role == "farmer" && 
            order.OrderItems.Any(oi => oi.Product.FarmerId == userId);
        
        if (!isBuyer && !isFarmer) return Forbid();
        
        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Where(m => m.OrderId == orderId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                OrderId = m.OrderId,
                SenderId = m.SenderId,
                SenderName = m.Sender.Name,
                SenderRole = m.Sender.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
            
        return Ok(messages);
    }
    
    // POST: api/orders/5/messages
    [HttpPost]
    public async Task<ActionResult<MessageDto>> SendMessage(int orderId, SendMessageDto dto)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null) return Unauthorized();
        
        // Verify user has access to this order
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);
            
        if (order == null) return NotFound();
        
        // User must be the order owner OR a farmer with products in this order
        var user = await _db.Users.FindAsync(userId);
        var isBuyer = order.UserId == userId;
        var isFarmer = user?.Role == "farmer" && 
            order.OrderItems.Any(oi => oi.Product.FarmerId == userId);
        
        if (!isBuyer && !isFarmer) return Forbid();
        
        var message = new Message
        {
            OrderId = orderId,
            SenderId = userId.Value,
            Content = dto.Content
        };
        
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
        
        return Ok(new MessageDto
        {
            Id = message.Id,
            OrderId = message.OrderId,
            SenderId = message.SenderId,
            SenderName = user!.Name,
            SenderRole = user.Role,
            Content = message.Content,
            CreatedAt = message.CreatedAt
        });
    }
}
