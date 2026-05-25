namespace OrderService.Application.Interfaces;

/// <summary>
/// Abstrai o commit da transação.
/// Cada use case (handler) chama SaveChangesAsync uma única vez ao final.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}