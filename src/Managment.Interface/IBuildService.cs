using System;
using System.Threading.Tasks;

namespace Managment.Interface
{
    public interface IBuildService : IDisposable
    {
        public Task TryExecute();
        public Task PublishAsync();
    }
}
