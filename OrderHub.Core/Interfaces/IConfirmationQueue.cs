using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Core.Interfaces;

public interface IConfirmationQueue
{
    Task EnqueueEmailAsync(string parentEmail, decimal totalAmount, CancellationToken cancellationToken);
}
