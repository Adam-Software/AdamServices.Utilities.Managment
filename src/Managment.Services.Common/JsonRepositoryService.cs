using Managment.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Managment.Services.Common
{
    public class JsonRepositoryService : IJsonRepositoryService
    {
        
        #region Var

        private readonly string mDownloadPath = "repositories";

        private readonly JsonSerializerOptions mJsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };

        #endregion

        #region ~

        public JsonRepositoryService(IServiceProvider serviceProvider)
        {
            var appSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();
            mDownloadPath = appSettingsOptionsService.UpdateServiceSettings.RepositoriesDownloadPath;
        }

        #endregion

        #region Public fields

        public string DownloadPath => mDownloadPath;

        #endregion

        #region Public methods

        public async Task SaveRawJsonFilesAsync(string content, string fileName)
        {
            if (!Directory.Exists(mDownloadPath))
            {
                Directory.CreateDirectory(mDownloadPath);
            }

            string filePath = Path.Combine(mDownloadPath, fileName);
            await File.WriteAllTextAsync(filePath, content);
        }

        public async Task SerializeAndSaveJsonFilesAsync<T>(T content, string fileName) where T : class
        {
            if (!Directory.Exists(mDownloadPath))
            {
                Directory.CreateDirectory(mDownloadPath);
            }

            string filePath = Path.Combine(mDownloadPath, fileName);

            string jsonString = JsonSerializer.Serialize<T>(content, mJsonSerializerOptions);
            await File.WriteAllTextAsync(filePath, jsonString);
        }

        public async Task SerializeAndSaveJsonFilesAsync<T>(T content, string path, string fileName) where T : class
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filePath = Path.Combine(path, fileName);

            string jsonString = JsonSerializer.Serialize<T>(content, mJsonSerializerOptions);
            await File.WriteAllTextAsync(filePath, jsonString);
        }

        public async Task<T> ReadJsonFileAsync<T>(string fileName) where T : class
        {
            string filePath = Path.Combine(mDownloadPath, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(jsonString, mJsonSerializerOptions);
        }

        public async Task<T> ReadJsonFileAsync<T>(string path, string fileName) where T : class
        {
            string filePath = Path.Combine(path, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(jsonString, mJsonSerializerOptions);
        }

        #endregion
    }
}
