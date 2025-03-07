using System;
using System.Threading.Tasks;

namespace Managment.Interface
{
    public interface IDotnetService : IDisposable
    {
        public Task PublishAsync();
    }
}
