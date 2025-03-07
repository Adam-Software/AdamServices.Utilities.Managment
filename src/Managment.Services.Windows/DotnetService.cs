using Managment.Interface;
using Managment.Interface.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Managment.Services.Windows
{
    public class DotnetService : IDotnetService
    {
        #region Services

        private readonly ILogger<DotnetService> mLogger;

        #endregion

        #region Const

        

        #endregion

        #region Var

        private readonly string mSourceBuildPath = "build";
        private readonly string mPublishPath = "release";
        #endregion

        #region ~

        public DotnetService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<DotnetService>>();

            IAppSettingsOptionsService appSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();
            mSourceBuildPath = appSettingsOptionsService.DownloadServiceSettings.SourceBuildPath;
            mPublishPath = appSettingsOptionsService.DotnetServiceSettings.PublishPath;

            mLogger.LogInformation("=== BuildService. Start ===");
        }

        #endregion

        #region Public methods

        public async Task PublishAsync()
        {
            List<string> sourcePaths = await JsonUtilites.ReadJsonFileAsync<List<string>>(mSourceBuildPath, "build-target-path-list.json");

            foreach (string sourcePath in sourcePaths)
            {
                await StartDotnetPublishProcessAsync(sourcePath);
            }
        }

        public void Dispose()
        {
            mLogger.LogInformation("=== BuildService. Dispose ===");
        }

        #endregion

        #region Private mehods

        private async Task StartDotnetPublishProcessAsync(string sourcePath)
        {
            var sourcePathTemp = Path.Combine(mSourceBuildPath, sourcePath);
            var sourceFullPath = new DirectoryInfo(sourcePathTemp).FullName;
            mLogger.LogTrace("{buildFullPath}", sourceFullPath);

            string publishPathTemp = Path.Combine(mPublishPath, sourcePath);
            var publishFullPath = new DirectoryInfo(publishPathTemp).FullName;
            string args = string.Format($"publish --source {sourceFullPath} --output {publishFullPath}");

            try
            {
                DirectoryUtilites.CreateOrClearDirectory(publishFullPath);
                await StartProcessAsync("dotnet", sourceFullPath, args);
            }
            catch (Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }
        }

        private Task StartProcessAsync(string command = "dotnet", string workingDirectory = "", string arguments = "")
        {
            ProcessStartInfo proccesInfo = new()
            {
                FileName = command,
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            Process process = new()
            {
                StartInfo = proccesInfo,
                EnableRaisingEvents = true,
            };

            process.Exited += ProcessExited;
            process.OutputDataReceived += ProcessOutputDataReceived;
            process.ErrorDataReceived += ProcessErrorDataReceived;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            return process.WaitForExitAsync();
        }

        private void ProcessExited(object? sender, EventArgs e)
        {
            mLogger.LogInformation("MSBUILD: exited"); 
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var message = e.Data;

            if(!string.IsNullOrEmpty(message))
                mLogger.LogInformation("MSBUILD: {data}", e.Data);
        }

        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var message = e.Data;

            if (!string.IsNullOrEmpty(message))
                mLogger.LogError("MSBUILD: {data}", e.Data);
        }

        #endregion
    }
}
