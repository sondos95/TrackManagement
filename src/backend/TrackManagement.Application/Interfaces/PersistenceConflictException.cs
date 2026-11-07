namespace TrackManagement.Application.Interfaces;

public enum PersistenceConflict
{
    UniqueConstraint,
    ConcurrentUpdate,
    RelatedResourceChanged
}

public sealed class PersistenceConflictException(PersistenceConflict kind, Exception innerException)
    : Exception("The changes could not be saved because the data conflicts with the current database state.", innerException)
{
    public PersistenceConflict Kind { get; } = kind;
}
