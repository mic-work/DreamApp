using DreamApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DreamApp.Api.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Gift> Gifts => Set<Gift>();
    public DbSet<WishlistAccess> WishlistAccesses => Set<WishlistAccess>();

    // IAppDbContext explicit implementation: exposes the same DbSets as
    // plain IQueryable<T> so controllers can depend on the interface while
    // this class still exposes the richer DbSet<T> API to the rest of the
    // EF Core infrastructure (migrations, OnModelCreating, etc.).
    IQueryable<User> IAppDbContext.Users => Users;
    IQueryable<Wishlist> IAppDbContext.Wishlists => Wishlists;
    IQueryable<Gift> IAppDbContext.Gifts => Gifts;
    IQueryable<WishlistAccess> IAppDbContext.WishlistAccesses => WishlistAccesses;

    void IAppDbContext.Add<TEntity>(TEntity entity) => Add(entity);
    void IAppDbContext.Remove<TEntity>(TEntity entity) => Remove(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----- User -----
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // ----- Wishlist -----
        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasIndex(w => w.OwnerId);

            entity.HasOne(w => w.Owner)
                  .WithMany(u => u.Wishlists)
                  .HasForeignKey(w => w.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ----- Gift -----
        modelBuilder.Entity<Gift>(entity =>
        {
            entity.HasIndex(g => g.WishlistId);

            entity.HasOne(g => g.Wishlist)
                  .WithMany(w => w.Gifts)
                  .HasForeignKey(g => g.WishlistId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Store the enum as a readable string ("Low"/"Medium"/"High")
            // instead of an opaque integer so the raw .db file stays legible.
            entity.Property(g => g.Priority)
                  .HasConversion<string>()
                  .HasMaxLength(16);
        });

        // ----- WishlistAccess -----
        modelBuilder.Entity<WishlistAccess>(entity =>
        {
            // A user can be granted access to a given wishlist only once.
            entity.HasIndex(a => new { a.UserId, a.WishlistId }).IsUnique();

            entity.HasOne(a => a.User)
                  .WithMany(u => u.WishlistAccesses)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Wishlist)
                  .WithMany(w => w.AccessGrants)
                  .HasForeignKey(a => a.WishlistId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
