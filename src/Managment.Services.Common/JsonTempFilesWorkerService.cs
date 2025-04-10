using Managment.Interface;
using Managment.Interface.Common;
using Managment.Interface.Common.JsonModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Managment.Services.Common
{
    public class JsonTempFilesWorkerService : IJsonTempFilesWorkerService
    {
        #region Var

        private readonly JsonSerializerOptions mJsonSerializerOptions = new();

        private readonly string mTempFilePath;
        private readonly string mPublishDirrectory;

        #endregion

        #region ~

        public JsonTempFilesWorkerService(IServiceProvider serviceProvider) 
        {
            IAppSettingsOptionsService appSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();

            string workingDirrectory = appSettingsOptionsService.WorkingDirrectory;
            string tempFileName = CommonFilesAndDirectoriesNames.TempInfoJsonFileName;
            string tempFilePath = Path.Combine(workingDirrectory, tempFileName);

            mTempFilePath = new DirectoryInfo(tempFilePath).FullName;

            string publishDirrectory = Path.Combine(appSettingsOptionsService.WorkingDirrectory, CommonFilesAndDirectoriesNames.PublishDirrectory);
            mPublishDirrectory = new DirectoryInfo(publishDirrectory).FullName;

            mJsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            mJsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            mJsonSerializerOptions.WriteIndented = true;
            mJsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
     
            CreateTempDirrectory(workingDirrectory);
        }

        #endregion

        #region Public methods

        public async Task<ServiceRepositories> ReadTempFileAsync()
        {
            if (!File.Exists(mTempFilePath))
            {
                throw new FileNotFoundException($"File not found: {mTempFilePath}");
            }

            string jsonString = await File.ReadAllTextAsync(mTempFilePath);
            return JsonSerializer.Deserialize<ServiceRepositories>(jsonString, mJsonSerializerOptions);
        }

        public async Task SaveTempFileAsync(ServiceRepositories content)
        {
            string jsonString = JsonSerializer.Serialize(content, mJsonSerializerOptions);
            await File.WriteAllTextAsync(mTempFilePath, jsonString);
        }

        /// <summary>
        /// Read service_info.json from published dirrectory by repository name
        /// </summary>
        public async Task<ServiceInfoModel> ReadPublishedProjectFileInfoAsync(string repositoriesName)
        {
            string publishDirrectory = Path.Combine(mPublishDirrectory, repositoriesName);
            string publishedProjectFileInfoPath = Path.Combine(publishDirrectory, CommonFilesAndDirectoriesNames.ServiceInfoFileName);

            if (!File.Exists(publishedProjectFileInfoPath))
            {
                throw new FileNotFoundException($"File not found: {publishedProjectFileInfoPath}");
            }

            byte[] installedFileInfoJson = await File.ReadAllBytesAsync(Path.Combine(publishDirrectory, CommonFilesAndDirectoriesNames.ServiceInfoFileName));
            return SerializeJson<ServiceInfoModel>(installedFileInfoJson);
        }

        /// <summary>
        /// Read service_info.json from all subdirrectory in published dirrectory 
        /// </summary>
        /// <returns>Project subdirectory name, ServiceInfoModel dictonary</Project></returns>
        public async Task<Dictionary<string, ServiceInfoModel>> ReadPublishedProjectFileInfoAsync()
        {
            Dictionary<string, ServiceInfoModel> serviceInfoModels = [];
            string[] projectDirectories = Directory.GetDirectories(mPublishDirrectory);

            foreach (var projectDirectory in projectDirectories) 
            {
                string publishedProjectFileInfoPath = Path.Combine(projectDirectory, CommonFilesAndDirectoriesNames.ServiceInfoFileName);

                if (!File.Exists(publishedProjectFileInfoPath))
                {
                    continue;
                }

                byte[] installedFileInfoJson = await File.ReadAllBytesAsync(Path.Combine(projectDirectory, CommonFilesAndDirectoriesNames.ServiceInfoFileName));
                var serviceInfoModel = SerializeJson<ServiceInfoModel>(installedFileInfoJson);
                serviceInfoModels.Add(projectDirectory, serviceInfoModel);
            }

            return serviceInfoModels;
        }

        public T SerializeJson<T>(byte[] content) where T : class, new()
        {
            try
            {
                T jsonObject = JsonSerializer.Deserialize<T>(content, mJsonSerializerOptions);
                return jsonObject;
            }
            catch
            {
                return new T();
            }
        }

        #endregion

        #region Private methods

        private static void CreateTempDirrectory(string workingDirrectory)
        {
            if (!Directory.Exists(workingDirrectory))
            {
                var workingDirrectoryPath = new DirectoryInfo(workingDirrectory).FullName;
                Directory.CreateDirectory(workingDirrectoryPath);
            }
        }

        #endregion
    }
}
