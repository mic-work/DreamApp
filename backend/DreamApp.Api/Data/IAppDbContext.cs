using DreamApp.Api.Models;

namespace DreamApp.Api.Data;

/// <summary>
/// Narrow abstraction over <see cref="AppDbContext"/> used by controllers.
/// It exists purely for testability: production code resolves the real
/// EF Core / SQLite backed <see cref="AppDbContext"/>, while unit tests can
/// substitute a lightweight in-memory fake (see DreamApp.Api.Tests/Fakes)
/// without spinning up a database. It intentionally mirrors DbContext's own
/// members (IQueryable sets, Add, Remove, SaveChangesAsync) rather than
/// introducing a separate repository-per-entity layer, to keep the
/// architecture simple as requested.
/// </summary>
public interface IAppDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<Wishlist> Wishlists { get; }
    IQueryable<Gift> Gifts { get; }
    IQueryable<WishlistAccess> WishlistAccesses { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
