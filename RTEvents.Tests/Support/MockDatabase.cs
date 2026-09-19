using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace RTEvents.Tests.Support;

// Only supplies LINQ-to-objects and mocked persistence; no EF provider or database is used.
internal sealed class MockDatabase : IDisposable
{
    public Mock<RTEventsDbContext> Context { get; } = new(new DbContextOptions<RTEventsDbContext>());
    public Mock<IDbContextTransaction> Transaction { get; } = new();
    public Mock<DatabaseFacade> Database { get; }

    public MockDatabase()
    {
        Database = new Mock<DatabaseFacade>(Context.Object);
        Database.Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Transaction.Object);
        Context.SetupGet(c => c.Database).Returns(Database.Object);
        Context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        Context.Object.Events = Set<Event>().Object;
        Context.Object.Venues = Set<Venue>().Object;
        Context.Object.Tickets = Set<Ticket>().Object;
        Context.Object.Payments = Set<Payment>().Object;
        Context.Object.Purchases = Set<Purchase>().Object;
        Context.Object.IdempotencyKeys = Set<IdempotencyKey>().Object;
        Context.Object.OutboxMessages = Set<OutboxMessage>().Object;
        Context.Object.Messages = Set<Message>().Object;
    }

    public static Mock<DbSet<T>> Set<T>(params T[] items) where T : class
    {
        IQueryable<T> query = new AsyncQuery<T>(items);
        var set = new Mock<DbSet<T>>();
        set.As<IQueryable<T>>().SetupGet(s => s.Provider).Returns(new AsyncProvider(query.Provider));
        set.As<IQueryable<T>>().SetupGet(s => s.Expression).Returns(query.Expression);
        set.As<IQueryable<T>>().SetupGet(s => s.ElementType).Returns(query.ElementType);
        set.As<IQueryable<T>>().Setup(s => s.GetEnumerator()).Returns(() => items.AsEnumerable().GetEnumerator());
        set.As<IAsyncEnumerable<T>>().Setup(s => s.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new AsyncEnumerator<T>(items.AsEnumerable().GetEnumerator()));
        set.Setup(s => s.FindAsync(It.IsAny<object?[]>())).Returns((object?[] keys) =>
            new ValueTask<T?>(items.SingleOrDefault(item => Equals(
                typeof(T).GetProperty(typeof(T) == typeof(IdempotencyKey) ? "Key" : "Id")!.GetValue(item), keys[0]))));
        return set;
    }

    public void VerifySaved(int times = 1) => Context.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(times));
    public void Dispose() => Context.Object.Dispose();
}

internal sealed class AsyncProvider(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => (IQueryable)Activator.CreateInstance(
        typeof(AsyncQuery<>).MakeGenericType(expression.Type.GetGenericArguments()[0]), expression)!;
    public IQueryable<T> CreateQuery<T>(Expression expression) => new AsyncQuery<T>(expression);
    public object? Execute(Expression expression) => inner.Execute(expression);
    public T Execute<T>(Expression expression) => inner.Execute<T>(expression);
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var result = typeof(IQueryProvider).GetMethod(nameof(Execute), 1, [typeof(Expression)])!
            .MakeGenericMethod(resultType).Invoke(inner, [expression]);
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, [result])!;
    }
}

internal sealed class AsyncQuery<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public AsyncQuery(IEnumerable<T> items) : base(items) { }
    public AsyncQuery(Expression expression) : base(expression) { }
    IQueryProvider IQueryable.Provider => new AsyncProvider(this);
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal sealed class AsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
    public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
}

internal static class Samples
{
    public static Event Event(int id = 7, int capacity = 10)
    {
        var result = new Event("Concert", "An evening concert", new DateOnly(2026, 10, 1), new TimeOnly(19, 30), "UTC", capacity, 2);
        Set(result, nameof(global::Event.Id), id);
        Set(result, nameof(global::Event.Venue), new Venue { Id = 2, Name = "The hall", Capacity = 100 });
        return result;
    }

    // Simulate values and navigation properties normally populated by EF, without changing domain visibility.
    public static T Set<T>(T entity, string property, object value)
    {
        typeof(T).GetProperty(property)!.SetValue(entity, value);
        return entity;
    }
}
