using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Core.Interfaces;

public interface ISchoolRepository
{
    Task<string?> GetSchoolTierAsync(int schoolId, CancellationToken cancellationToken);
}
