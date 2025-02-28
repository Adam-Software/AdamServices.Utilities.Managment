using System.Threading.Tasks;

namespace Managment.Interface
{
    public interface IJsonRepositoryService
    {
        #region Fields

        public string DownloadPath { get; }

        #endregion

        #region Methods

        public Task SaveRawJsonFilesAsync(string content, string fileName);
        public Task SerializeAndSaveJsonFilesAsync<T>(T content, string fileName) where T : class;
        public Task SerializeAndSaveJsonFilesAsync<T>(T content, string path, string fileName) where T : class;
        public Task<T> ReadJsonFileAsync<T>(string fileName) where T : class;
        public Task<T> ReadJsonFileAsync<T>(string path, string fileName) where T : class;

        #endregion
    }
}
