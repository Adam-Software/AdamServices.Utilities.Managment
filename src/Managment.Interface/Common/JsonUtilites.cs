using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Managment.Interface.Common
{
    public class JsonUtilites 
    {
        
        #region Var

        private static readonly JsonSerializerOptions mJsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };

        #endregion

        #region ~

        #endregion

        #region Public methods

        public static async Task SaveRawJsonFilesAsync(string content, string path, string fileName)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filePath = Path.Combine(path, fileName);
            await File.WriteAllTextAsync(filePath, content);
        }

        public static async Task SerializeAndSaveJsonFilesAsync<T>(T content, string path, string fileName) where T : class
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filePath = Path.Combine(path, fileName);

            string jsonString = JsonSerializer.Serialize<T>(content, mJsonSerializerOptions);
            await File.WriteAllTextAsync(filePath, jsonString);
        }

        public static async Task<T> ReadJsonFileAsync<T>(string path, string fileName) where T : class
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
