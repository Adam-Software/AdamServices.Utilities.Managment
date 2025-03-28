using Managment.Interface.AppSettingsOptionsServiceDependency;

namespace Managment.Interface
{
    public interface IAppSettingsOptionsService
    {
        public string WorkingDirrectory { get; set; } 
        public UpdateServiceSettings UpdateServiceSettings { get; set; }
        public GitHubClientServiceSettings GitHubClientServiceSettings { get; set; }
        public DotnetServiceSettings DotnetServiceSettings { set; get; }
    }
}
