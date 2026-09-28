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
    public DbSet<GiftPhoto> GiftPhotos => Set<GiftPhoto>();
    public DbSet<GiftVisibility> GiftVisibilities => Set<GiftVisibility>();
    public DbSet<GiftReception> GiftReceptions => Set<GiftReception>();

    // IAppDbContext explicit implementation: exposes the same DbSets as
    // plain IQueryable<T> so controllers can depend on the interface while
    // this class still exposes the richer DbSet<T> API to the rest of the
    // EF Core infrastructure (migrations, OnModelCreating, etc.).
    IQueryable<User> IAppDbContext.Users => Users;
    IQueryable<Wishlist> IAppDbContext.Wishlists => Wishlists;
    IQueryable<Gift> IAppDbContext.Gifts => Gifts;
    IQueryable<WishlistAccess> IAppDbContext.WishlistAccesses => WishlistAccesses;
    IQueryable<GiftPhoto> IAppDbContext.GiftPhotos => GiftPhotos;
    IQueryable<GiftVisibility> IAppDbContext.GiftVisibilities => GiftVisibilities;
    IQueryable<GiftReception> IAppDbContext.GiftReceptions => GiftReceptions;

    void IAppDbContext.Add<TEntity>(TEntity entity) => Add(entity);
    void IAppDbContext.Remove<TEntity>(TEntity entity) => Remove(entity);
    void IAppDbContext.RemoveRange<TEntity>(IEnumerable<TEntity> entities) => RemoveRange(entities);

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

        // ----- GiftPhoto -----
        modelBuilder.Entity<GiftPhoto>(entity =>
        {
            entity.HasIndex(p => p.GiftId);

            entity.HasOne(p => p.Gift)
                  .WithMany(g => g.Photos)
                  .HasForeignKey(p => p.GiftId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ----- GiftVisibility -----
        modelBuilder.Entity<GiftVisibility>(entity =>
        {
            // The same friend can only be listed once per gift.
            entity.HasIndex(v => new { v.GiftId, v.UserId }).IsUnique();

            entity.HasOne(v => v.Gift)
                  .WithMany(g => g.VisibleTo)
                  .HasForeignKey(v => v.GiftId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Restrict (not cascade): deleting a user should not silently
            // widen another user's gift visibility list. The application
            // layer cleans up stale rows explicitly if it ever needs to.
            entity.HasOne(v => v.User)
                  .WithMany()
                  .HasForeignKey(v => v.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ----- GiftReception -----
        modelBuilder.Entity<GiftReception>(entity =>
        {
            // Each friend has at most one reception record per gift.
            entity.HasIndex(r => new { r.GiftId, r.MarkedByUserId }).IsUnique();

            entity.HasOne(r => r.Gift)
                  .WithMany(g => g.Receptions)
                  .HasForeignKey(r => r.GiftId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.MarkedByUser)
                  .WithMany()
                  .HasForeignKey(r => r.MarkedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.GivenByUser)
                  .WithMany()
                  .HasForeignKey(r => r.GivenByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
