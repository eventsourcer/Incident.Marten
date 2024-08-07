using Marten;

namespace Incident.Api.Extensions;

public static class DocumentSessionExtensions
{
    public static async Task Add<T>(
        this IDocumentSession session,
        Guid id,object @event,
        CancellationToken ct
    ) where T : class
    {
        session.Events.StartStream<T>(id, @event);
        await session.SaveChangesAsync(ct);
    }
    public static async Task GetAnUpdate_Detailed<T>(
        this IDocumentSession session,
        Guid id,
        int version,
        Func<T, object> handle,
        CancellationToken ct
    ) where T : class
    {
        var aggregate = await session.Events.AggregateStreamAsync<T>(id, token: ct) 
            ?? throw new InvalidOperationException($"{typeof(T).Name} with id {id} wasn't found");

        var @event = handle(aggregate);
        session.Events.Append(id, version + 1, @event);
        await session.SaveChangesAsync(ct);
    }
    // this short method encapsulates above logic 
    public static async Task GetAnUpdate<T>(
        this IDocumentSession session,
        Guid id,
        int version,
        Func<T, object> handle,
        CancellationToken ct
    ) where T : class =>
    await session.Events.WriteToAggregate<T>(id, version, stream =>
        stream.AppendOne(handle(stream.Aggregate)), ct);
}