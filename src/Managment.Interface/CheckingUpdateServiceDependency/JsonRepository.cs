using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Managment.Interface.CheckingUpdateServiceDependency
{
    public class JsonRepository
    {
        #region Var

        private readonly string mRepositoryPath;

        private readonly JsonSerializerOptions mJsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };

        #endregion

        #region ~

        public JsonRepository(string repositoryPath)
        {
            mRepositoryPath = repositoryPath;
        }

        #endregion

        #region Public methods

        public async Task SaveRawJsonFilesAsync(string content, string fileName)
        {
            if (!Directory.Exists(mRepositoryPath))
            {
                Directory.CreateDirectory(mRepositoryPath);
            }

            string filePath = Path.Combine(mRepositoryPath, fileName);
            await File.WriteAllTextAsync(filePath, content);
        }

        public async Task SerializeAndSaveJsonFilesAsync<T>(T content, string fileName) where T : class
        {
            if (!Directory.Exists(mRepositoryPath))
            {
                Directory.CreateDirectory(mRepositoryPath);
            }

            string filePath = Path.Combine(mRepositoryPath, fileName);

            string jsonString = JsonSerializer.Serialize<T>(content, mJsonSerializerOptions);
            await File.WriteAllTextAsync(filePath, jsonString);
        }

        public async Task<T> ReadJsonFileAsync<T>(string fileName) where T : class
        {
            string filePath = Path.Combine(mRepositoryPath, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(jsonString, mJsonSerializerOptions);
        }

        public void CreateOrClearRepositoryDirectory()
        {
            if (!Directory.Exists(mRepositoryPath))
            {
                Directory.CreateDirectory(mRepositoryPath);
                return;
            }

            string[] files = Directory.GetFiles(mRepositoryPath);
            foreach (string file in files)
            {
                File.Delete(file);
            }
        }

        #endregion
    }
}
