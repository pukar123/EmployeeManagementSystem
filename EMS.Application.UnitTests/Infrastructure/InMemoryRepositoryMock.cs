using System.Linq.Expressions;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable;
using Moq;

namespace EMS.Application.UnitTests.Infrastructure;

/// <summary>
/// Builds a <see cref="IBaseRepository{T}"/> backed by an in-memory list with EF-async-capable queryables.
/// </summary>
public sealed class InMemoryRepositoryMock<T> where T : class
{
    private readonly List<T> _items = [];
    private readonly Func<T, int> _getId;
    private readonly Action<T, int> _setId;

    public InMemoryRepositoryMock(Func<T, int> getId, Action<T, int> setId)
    {
        _getId = getId;
        _setId = setId;
    }

    public IReadOnlyList<T> Items => _items;

    public void Seed(params T[] items) => _items.AddRange(items);

    public Mock<IBaseRepository<T>> CreateMock()
    {
        var mock = new Mock<IBaseRepository<T>>();

        mock.Setup(r => r.GetQueryable()).Returns(() => _items.BuildMock());
        mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => _items.ToList());
        mock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => _items.FirstOrDefault(x => _getId(x) == id));
        mock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Expression<Func<T, bool>> pred, CancellationToken _) =>
            {
                var compiled = pred.Compile();
                return Task.FromResult<IEnumerable<T>>(_items.Where(compiled).ToList());
            });
        mock.Setup(r => r.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((e, _) => _items.Add(e))
            .Returns(Task.CompletedTask);
        mock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<T>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<T>, CancellationToken>((e, _) => _items.AddRange(e))
            .Returns(Task.CompletedTask);
        mock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            var next = _items.Count == 0 ? 1 : _items.Max(_getId) + 1;
            foreach (var e in _items.Where(x => _getId(x) == 0).ToList())
                _setId(e, next++);
            return Task.CompletedTask;
        });
        mock.Setup(r => r.Update(It.IsAny<T>())).Callback<T>(_ => { });
        mock.Setup(r => r.Remove(It.IsAny<T>())).Callback<T>(e => _items.Remove(e));
        mock.Setup(r => r.RemoveRange(It.IsAny<IEnumerable<T>>()))
            .Callback<IEnumerable<T>>(range =>
            {
                foreach (var e in range.ToList())
                    _items.Remove(e);
            });
        mock.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => CreateTransaction().Object);

        return mock;
    }

    private static Mock<IDbContextTransaction> CreateTransaction()
    {
        var tx = new Mock<IDbContextTransaction>();
        tx.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        tx.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        tx.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        tx.Setup(t => t.Dispose());
        return tx;
    }
}
