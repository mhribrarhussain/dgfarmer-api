using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DgFarmerApi.Data;
using DgFarmerApi.Models;
using DgFarmerApi.DTOs;
using DgFarmerApi.Services;

namespace DgFarmerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthService _authService;
    
    public AuthController(AppDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }
    
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        // Check if email exists
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }
        
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            Phone = dto.Phone,
            Address = dto.Address
        };
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        
        var token = _authService.GenerateToken(user);
        
        return Ok(new AuthResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Token = token
        });
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }
        
        var token = _authService.GenerateToken(user);
        
        return Ok(new AuthResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Token = token
        });
    }
    
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null) return Unauthorized();
        
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        
        return Ok(new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Phone = user.Phone,
            Address = user.Address,
            Avatar = user.Avatar
        });
    }
}
