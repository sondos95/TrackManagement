namespace TrackManagement.Application.Features.Tracks;

public enum TrackErrorKind { InvalidInput, NotFound, Conflict }
public sealed record TrackError(TrackErrorKind Kind, string Message);

public sealed class TrackResult<T> where T : class
{
    private TrackResult(T? value, TrackError? error) => (Value, Error) = (value, error);

    public T? Value { get; }
    public TrackError? Error { get; }

    public static TrackResult<T> Success(T value) => new(value, null);
    public static TrackResult<T> Fail(TrackErrorKind kind, string message) => new(null, new(kind, message));
}
