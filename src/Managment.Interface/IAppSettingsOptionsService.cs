namespace Managment.Interface
{
    public interface IAppSettingsOptionsService
    {
        public string CheckUpdateUrl { get; set; }

        public string JsonRepositoryPath { get; set; }
    }
}
