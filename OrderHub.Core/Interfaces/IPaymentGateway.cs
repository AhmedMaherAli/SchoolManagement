using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Core.Interfaces;

public interface IPaymentGateway
{
    Task<bool> ProcessPaymentAsync(decimal amount, string email, CancellationToken cancellationToken);
}
