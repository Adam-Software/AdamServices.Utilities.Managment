using Managment.Interface.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Managment.Interface.CheckingUpdateServiceDependency
{
    public class JsonRepository
    {
        #region Var

        private readonly string mRepositoryPath;

        private readonly JsonSerializerOptions jsonSerializerOptions = new()
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

        public async Task<bool> SaveJsonFilesAsync(string content, string fileName)
        {
            try
            {
                if (!Directory.Exists(mRepositoryPath))
                {
                    Directory.CreateDirectory(mRepositoryPath);
                }

                string filePath = Path.Combine(mRepositoryPath, fileName);
                await File.WriteAllTextAsync(filePath, content);
                return true;
            }
            catch 
            {
                return false;
            }
        }

        public async Task<T> ReadJsonFileAsync<T>(string fileName) where T : class
        {
            string filePath = Path.Combine(mRepositoryPath, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(jsonString, jsonSerializerOptions);
        }

        /*public async Task SaveJsonResponseAsync(string fileName, string url)
        {
            if (!Directory.Exists(mRepositoryPath))
            {
                Directory.CreateDirectory(mRepositoryPath);
            }

            using HttpClient client = new();

            string jsonResponseServiceUrls = await client.GetStringAsync(url);
            string filePath = Path.Combine(mRepositoryPath, fileName);

            File.WriteAllText(filePath, jsonResponseServiceUrls);
        }

        public void SerializeAndSaveServiceUrls(List<ServiceUrlModel> serviceUrls, string fileName)
        {
            if (!Directory.Exists(mRepositoryPath))
            {
                Directory.CreateDirectory(mRepositoryPath);
            }

            string filePath = Path.Combine(mRepositoryPath, fileName);

            string jsonString = JsonSerializer.Serialize(serviceUrls, jsonSerializerOptions);
            File.WriteAllText(filePath, jsonString);
        }

        public void SerializeAndSaveServiceNameWithUrls(List<ServiceNameWithUrl> serviceUrls, string fileName)
        {
            if (!Directory.Exists(mRepositoryPath))
            {
                Directory.CreateDirectory(mRepositoryPath);
            }

            string filePath = Path.Combine(mRepositoryPath, fileName);

            string jsonString = JsonSerializer.Serialize(serviceUrls, jsonSerializerOptions);
            File.WriteAllText(filePath, jsonString);
        }

        public async Task<T> ReadJsonFileAsync<T>(string fileName) where T : class
        {
            string filePath = Path.Combine(mRepositoryPath, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(jsonString, jsonSerializerOptions);
        }*/

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
    }
}
