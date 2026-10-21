using Microsoft.EntityFrameworkCore;
using ByteStoreAPI.Models;

namespace ByteStoreAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<VerificationCode> VerificationCodes { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("A_Users");
            modelBuilder.Entity<Category>().ToTable("A_Categories");
            modelBuilder.Entity<Manufacturer>().ToTable("A_Manufacturers");
            modelBuilder.Entity<Supplier>().ToTable("A_Suppliers");
            modelBuilder.Entity<Product>().ToTable("A_Products");
            modelBuilder.Entity<Cart>().ToTable("A_Carts");
            modelBuilder.Entity<CartItem>().ToTable("A_CartItems");
            modelBuilder.Entity<Order>().ToTable("A_Orders");
            modelBuilder.Entity<OrderItem>().ToTable("A_OrderItems");
            modelBuilder.Entity<VerificationCode>().ToTable("A_VerificationCodes");
            modelBuilder.Entity<Message>().ToTable("A_Messages");

            modelBuilder.Entity<User>().Property(u => u.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Category>().Property(c => c.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Manufacturer>().Property(m => m.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Supplier>().Property(s => s.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Product>().Property(p => p.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Cart>().Property(c => c.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<CartItem>().Property(ci => ci.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Order>().Property(o => o.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<OrderItem>().Property(oi => oi.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<VerificationCode>().Property(v => v.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<Message>().Property(m => m.Id).ValueGeneratedOnAdd();

            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}