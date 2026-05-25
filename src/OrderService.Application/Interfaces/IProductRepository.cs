using OrderService.Domain.Entities;

namespace OrderService.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carrega múltiplos produtos de uma vez para evitar N+1.
    /// </summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    void Update(Product product);
}