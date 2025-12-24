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
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthService _authService;
    
    public ProductsController(AppDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }
    
    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null)
    {
        var query = _db.Products
            .Include(p => p.Farmer)
            .Where(p => p.IsActive);
            
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category.ToLower() == category.ToLower());
        }
        
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));
        }
        
        var products = await query.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Category = p.Category,
            Image = p.Image,
            Unit = p.Unit,
            Stock = p.Stock,
            Rating = p.Rating,
            FarmerName = p.Farmer.Name,
            FarmerId = p.FarmerId
        }).ToListAsync();
        
        return Ok(products);
    }
    
    // GET: api/products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _db.Products
            .Include(p => p.Farmer)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (product == null) return NotFound();
        
        return Ok(new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Category = product.Category,
            Image = product.Image,
            Unit = product.Unit,
            Stock = product.Stock,
            Rating = product.Rating,
            FarmerName = product.Farmer.Name,
            FarmerId = product.FarmerId
        });
    }
    
    // GET: api/products/categories
    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = await _db.Products
            .Where(p => p.IsActive)
            .Select(p => p.Category)
            .Distinct()
            .ToListAsync();
            
        return Ok(categories);
    }
    
    // GET: api/products/featured
    [HttpGet("featured")]
    public async Task<ActionResult<List<ProductDto>>> GetFeatured()
    {
        // Fetch all active products first to avoid SQLite translation issues with complex ordering
        var allProducts = await _db.Products
            .Include(p => p.Farmer)
            .Where(p => p.IsActive)
            .ToListAsync();
            
        var featuredProducts = allProducts
            .OrderByDescending(p => p.Rating)
            .Take(6)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Category = p.Category,
                Image = p.Image,
                Unit = p.Unit,
                Stock = p.Stock,
                Rating = p.Rating,
                FarmerName = p.Farmer.Name,
                FarmerId = p.FarmerId
            }).ToList();
            
        return Ok(featuredProducts);
    }
    
    // POST: api/products (Farmer only)
    [HttpPost]
    [Authorize(Roles = "farmer")]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null) return Unauthorized();
        
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Category = dto.Category,
            Image = dto.Image,
            Unit = dto.Unit,
            Stock = dto.Stock,
            FarmerId = userId.Value
        };
        
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        
        var farmer = await _db.Users.FindAsync(userId);
        
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Category = product.Category,
            Image = product.Image,
            Unit = product.Unit,
            Stock = product.Stock,
            Rating = product.Rating,
            FarmerName = farmer?.Name ?? "",
            FarmerId = product.FarmerId
        });
    }
    
    // PUT: api/products/5 (Farmer only)
    [HttpPut("{id}")]
    [Authorize(Roles = "farmer")]
    public async Task<IActionResult> Update(int id, CreateProductDto dto)
    {
        var userId = _authService.GetUserIdFromToken(User);
        var product = await _db.Products.FindAsync(id);
        
        if (product == null) return NotFound();
        if (product.FarmerId != userId) return Forbid();
        
        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Category = dto.Category;
        product.Image = dto.Image;
        product.Unit = dto.Unit;
        product.Stock = dto.Stock;
        
        await _db.SaveChangesAsync();
        return NoContent();
    }
    
    // DELETE: api/products/5 (Farmer only)
    [HttpDelete("{id}")]
    [Authorize(Roles = "farmer")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _authService.GetUserIdFromToken(User);
        var product = await _db.Products.FindAsync(id);
        
        if (product == null) return NotFound();
        if (product.FarmerId != userId) return Forbid();
        
        product.IsActive = false; // Soft delete
        await _db.SaveChangesAsync();
        return NoContent();
    }
    
    // GET: api/products/my-products (Farmer's own products)
    [HttpGet("my-products")]
    [Authorize(Roles = "farmer")]
    public async Task<ActionResult<List<ProductDto>>> GetMyProducts()
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null) return Unauthorized();
        
        var products = await _db.Products
            .Include(p => p.Farmer)
            .Where(p => p.FarmerId == userId && p.IsActive)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Category = p.Category,
                Image = p.Image,
                Unit = p.Unit,
                Stock = p.Stock,
                Rating = p.Rating,
                FarmerName = p.Farmer.Name,
                FarmerId = p.FarmerId
            }).ToListAsync();
            
        return Ok(products);
    }
}
