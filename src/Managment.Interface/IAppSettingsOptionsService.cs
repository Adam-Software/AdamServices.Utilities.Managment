using Managment.Interface.AppSettingsOptionsServiceDependency;

namespace Managment.Interface
{
    public interface IAppSettingsOptionsService
    {
        public UpdateServiceSettings UpdateServiceSettings { get; set; }
        public GitHubClientServiceSettings GitHubClientServiceSettings { get; set; }
        public DownloadServiceSettings DownloadServiceSettings { set; get; }
        public DotnetServiceSettings DotnetServiceSettings { set; get; }
    }
}
