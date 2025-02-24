using Managment.Interface;
using Managment.Interface.AppSettingsOptionsServiceDependency;

namespace Managment.Core.Services
{
    public class AppSettingsOptionsService : IAppSettingsOptionsService
    {
        public UpdateServiceSettings UpdateServiceSettings { get ; set ; }
    }
}
