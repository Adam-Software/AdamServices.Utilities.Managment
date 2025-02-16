using Managment.Interface;

namespace Managment.Core
{
    public class AppSettingsOptionsService : IAppSettingsOptionsService
    {
        public string CheckUpdateUrl { get; set; }
        public string JsonRepositoryPath {  get; set; }
    }
}
