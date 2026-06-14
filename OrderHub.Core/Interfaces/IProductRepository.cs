using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Core.Models;

namespace OrderHub.Core.Interfaces;

public interface IProductRepository
{
    // Fetches all required SKUs in a single round-trip.
    Task<Dictionary<string, ProductDetails>> GetProductsBySkusAsync(
        IEnumerable<string> skus,
        CancellationToken cancellationToken);
}
