using System.Threading.Tasks;

namespace Managment.Interface
{
    public interface IJsonRepositoryService
    {
        #region Methods

        public Task SaveRawJsonFilesAsync(string content, string path, string fileName);
        public Task SerializeAndSaveJsonFilesAsync<T>(T content, string path, string fileName) where T : class;
        public Task<T> ReadJsonFileAsync<T>(string path, string fileName) where T : class;

        #endregion
    }
}
