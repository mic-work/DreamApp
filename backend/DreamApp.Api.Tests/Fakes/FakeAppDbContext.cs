using DreamApp.Api.Data;
using DreamApp.Api.Models;

namespace DreamApp.Api.Tests.Fakes;

/// <summary>
/// Lightweight in-memory stand-in for <see cref="AppDbContext"/>, used by
/// the fast unit tests in this project. It only needs to satisfy
/// <see cref="IAppDbContext"/>, so it has zero dependency on Entity
/// Framework or SQLite: no real database is ever created for these tests.
/// Slower, fully realistic tests that exercise the real EF Core + SQLite
/// stack go through <see cref="Integration.CustomWebApplicationFactory"/> instead.
/// </summary>
public class FakeAppDbContext : IAppDbContext
{
    private readonly List<User> _users = new();
    private readonly List<Wishlist> _wishlists = new();
    private readonly List<Gift> _gifts = new();
    private readonly List<WishlistAccess> _wishlistAccesses = new();

    private int _nextUserId = 1;
    private int _nextWishlistId = 1;
    private int _nextGiftId = 1;
    private int _nextAccessId = 1;

    public IQueryable<User> Users => _users.AsQueryable();
    public IQueryable<Wishlist> Wishlists => _wishlists.AsQueryable();
    public IQueryable<Gift> Gifts => _gifts.AsQueryable();
    public IQueryable<WishlistAccess> WishlistAccesses => _wishlistAccesses.AsQueryable();

    public void Add<TEntity>(TEntity entity) where TEntity : class
    {
        switch (entity)
        {
            case User user:
                user.Id = _nextUserId++;
                _users.Add(user);
                break;

            case Wishlist wishlist:
                wishlist.Id = _nextWishlistId++;
                // Mimic EF Core's relationship fixup: setting the Owner navigation
                // property is enough, the FK value is resolved automatically.
                if (wishlist.OwnerId == 0 && wishlist.Owner is not null)
                {
                    wishlist.OwnerId = wishlist.Owner.Id;
                }
                _wishlists.Add(wishlist);
                break;

            case Gift gift:
                gift.Id = _nextGiftId++;
                _gifts.Add(gift);
                break;

            case WishlistAccess access:
                access.Id = _nextAccessId++;
                _wishlistAccesses.Add(access);
                break;

            default:
                throw new NotSupportedException($"FakeAppDbContext does not know how to add a {typeof(TEntity).Name}.");
        }
    }

    public void Remove<TEntity>(TEntity entity) where TEntity : class
    {
        switch (entity)
        {
            case User user: _users.Remove(user); break;
            case Wishlist wishlist: _wishlists.Remove(wishlist); break;
            case Gift gift: _gifts.Remove(gift); break;
            case WishlistAccess access: _wishlistAccesses.Remove(access); break;
            default:
                throw new NotSupportedException($"FakeAppDbContext does not know how to remove a {typeof(TEntity).Name}.");
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}
