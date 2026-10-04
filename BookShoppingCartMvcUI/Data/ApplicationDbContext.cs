using BookShoppingCartMvcUI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<CartDetail> CartDetails => Set<CartDetail>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // money is decimal, double can't hold 0.1 exactly so totals drift
        builder.Entity<Book>().Property(b => b.Price).HasPrecision(18, 2);
        builder.Entity<CartDetail>().Property(d => d.UnitPrice).HasPrecision(18, 2);
        builder.Entity<OrderDetail>().Property(d => d.UnitPrice).HasPrecision(18, 2);

        // soft deleted orders are hidden everywhere, IgnoreQueryFilters() if you ever need them.
        // order lines get the same filter, otherwise ef warns the required Order nav can be filtered out
        builder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
        builder.Entity<OrderDetail>().HasQueryFilter(od => !od.Order.IsDeleted);

        // the admin page checks for a duplicate name, this stops two saves at the same time both getting through.
        // case insensitive like the check, since the default sql server collation is
        builder.Entity<Genre>().HasIndex(g => g.GenreName).IsUnique();

        // a status that orders use can't be deleted
        builder.Entity<Order>()
            .HasOne(o => o.OrderStatus)
            .WithMany()
            .HasForeignKey(o => o.OrderStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // wishlist rows get deleted with the user or the book
        builder.Entity<WishlistItem>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<WishlistItem>()
            .HasIndex(w => new { w.UserId, w.BookId })
            .IsUnique();

        // one review per user per book, deleted with the user or the book
        builder.Entity<Review>()
            .HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Review>()
            .HasIndex(r => new { r.UserId, r.BookId })
            .IsUnique();
        builder.Entity<Review>()
            .ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "[Rating] BETWEEN 1 AND 5"));

        // stock can't go below 0, whatever code is updating it
        builder.Entity<Stock>()
            .ToTable(t => t.HasCheckConstraint("CK_Stock_Quantity", "[Quantity] >= 0"));

        // don't cascade to order lines - deleting a book (or a genre) would wipe order history and change the revenue
        builder.Entity<OrderDetail>()
            .HasOne(od => od.Book)
            .WithMany(b => b.OrderDetail)
            .HasForeignKey(od => od.BookId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Book>()
            .HasOne(b => b.Genre)
            .WithMany(g => g.Books)
            .HasForeignKey(b => b.GenreId)
            .OnDelete(DeleteBehavior.Restrict);

        // carts and orders are always looked up by user id (cart count runs on every page) so index it.
        // can't index nvarchar(max), so use the same length as AspNetUsers.Id
        // no FK to the user on purpose, deleting an account shouldn't delete their orders
        builder.Entity<ShoppingCart>().Property(c => c.UserId).HasMaxLength(450);
        builder.Entity<Order>().Property(o => o.UserId).HasMaxLength(450);
        builder.Entity<Order>().HasIndex(o => o.UserId);

        // one cart per user and one line per book. without these a double click on add to cart
        // could create 2 carts/lines, and checkout only reads one of them
        builder.Entity<ShoppingCart>().HasIndex(c => c.UserId).IsUnique();
        builder.Entity<CartDetail>().HasIndex(d => new { d.ShoppingCartId, d.BookId }).IsUnique();
    }
}
