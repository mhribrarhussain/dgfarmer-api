using Microsoft.EntityFrameworkCore;
using DgFarmerApi.Models;

namespace DgFarmerApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User configuration
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
            
        // Product -> Farmer relationship
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Farmer)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.FarmerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Order -> User relationship
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // OrderItem relationships
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Seed sample data
        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed a sample farmer
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Name = "Ahmed Khan",
                Email = "farmer@dgfarmer.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("farmer123"),
                Role = "farmer",
                Phone = "+92 300 1234567",
                Address = "Punjab, Pakistan"
            },
            new User
            {
                Id = 2,
                Name = "Ali Hassan",
                Email = "buyer@dgfarmer.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("buyer123"),
                Role = "buyer",
                Phone = "+92 300 7654321",
                Address = "Lahore, Pakistan"
            }
        );
        
        // Seed sample products
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Fresh Organic Tomatoes", Description = "Locally grown organic tomatoes", Price = 120, Category = "vegetables", Unit = "kg", Stock = 50, Rating = 4.5m, FarmerId = 1, Image = "https://images.unsplash.com/photo-1546470427-227c7068e771?w=400" },
            new Product { Id = 2, Name = "Farm Fresh Eggs", Description = "Free-range chicken eggs", Price = 280, Category = "dairy", Unit = "dozen", Stock = 30, Rating = 4.8m, FarmerId = 1, Image = "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=400" },
            new Product { Id = 3, Name = "Organic Honey", Description = "Pure natural honey from local bees", Price = 850, Category = "other", Unit = "kg", Stock = 20, Rating = 4.9m, FarmerId = 1, Image = "https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400" },
            new Product { Id = 4, Name = "Fresh Spinach", Description = "Organic leafy spinach", Price = 60, Category = "vegetables", Unit = "bundle", Stock = 40, Rating = 4.3m, FarmerId = 1, Image = "https://images.unsplash.com/photo-1576045057995-568f588f82fb?w=400" },
            new Product { Id = 5, Name = "Red Apples", Description = "Sweet and crispy red apples", Price = 180, Category = "fruits", Unit = "kg", Stock = 35, Rating = 4.6m, FarmerId = 1, Image = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=400" },
            new Product { Id = 6, Name = "Basmati Rice", Description = "Premium quality basmati rice", Price = 320, Category = "grains", Unit = "kg", Stock = 100, Rating = 4.7m, FarmerId = 1, Image = "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=400" }
        );
    }
}
