using Managment.Interface;
using Managment.Interface.AppSettingsOptionsServiceDependency;

namespace Managment.Core.Services
{
    public class AppSettingsOptionsService : IAppSettingsOptionsService
    {
        public string WorkingDirrectory { get; set; } = "temp";
        public GitHubClientServiceSettings GitHubClientServiceSettings { get; set; }
        public UpdateServiceSettings UpdateServiceSettings { get; set; }
        public DotnetServiceSettings DotnetServiceSettings { get ; set ; }
    }
}
