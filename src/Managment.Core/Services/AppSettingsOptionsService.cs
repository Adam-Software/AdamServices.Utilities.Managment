using Managment.Interface;
using Managment.Interface.AppSettingsOptionsServiceDependency;

namespace Managment.Core.Services
{
    public class AppSettingsOptionsService : IAppSettingsOptionsService
    {
        public GitHubClientServiceSettings GitHubClientServiceSettings { get; set; }
        public UpdateServiceSettings UpdateServiceSettings { get; set; }
        public DownloadServiceSettings DownloadServiceSettings {  set; get; }
        public DotnetServiceSettings DotnetServiceSettings { get ; set ; }
    }
}
