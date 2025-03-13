using System;
using System.Threading;
using System.Threading.Tasks;

namespace Managment.Interface
{
    public interface IDotnetService : IDisposable
    {
        public Task PublishAsync(CancellationToken cancellationToken = default);
        public Task RunAsync(CancellationToken cancellationToken = default);
    }
}
