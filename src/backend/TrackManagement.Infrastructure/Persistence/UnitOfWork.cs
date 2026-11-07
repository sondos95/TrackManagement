using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrackManagement.Application.Interfaces;

namespace TrackManagement.Infrastructure.Persistence;

internal sealed class UnitOfWork(TrackManagementDbContext db) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            db.ChangeTracker.Clear();
            throw new PersistenceConflictException(PersistenceConflict.ConcurrentUpdate, exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException
            { SqliteExtendedErrorCode: 1555 or 2067 or 787 } sqlite)
        {
            db.ChangeTracker.Clear();
            var kind = sqlite.SqliteExtendedErrorCode == 787
                ? PersistenceConflict.RelatedResourceChanged
                : PersistenceConflict.UniqueConstraint;
            throw new PersistenceConflictException(kind, exception);
        }
    }
}
