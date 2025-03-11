using Managment.Interface;
using Managment.Interface.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Managment.Services.Common
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
        private readonly string mDotnetVerbosityLevel = "q";
        private readonly List<string> mBuildLogs = [];
        #endregion

        #region ~

        public DotnetService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<DotnetService>>();

            IAppSettingsOptionsService appSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();
            mSourceBuildPath = appSettingsOptionsService.DownloadServiceSettings.SourceBuildPath;
            mPublishPath = appSettingsOptionsService.DotnetServiceSettings.PublishPath;
            mDotnetVerbosityLevel = appSettingsOptionsService.DotnetServiceSettings.DotnetVerbosityLevel;

            mLogger.LogInformation("=== DotnetService. Start ===");
        }

        #endregion

        #region Public methods

        public async Task PublishAsync()
        {
            List<string> sourcePaths = await JsonUtilites.ReadJsonFileAsync<List<string>>(mSourceBuildPath, ServiceFileNames.BuildTargetPathList);

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

            string publishPathTemp = Path.Combine(mPublishPath, sourcePath);
            var publishFullPath = new DirectoryInfo(publishPathTemp).FullName;

            var dotnetVerbosityLevel = mDotnetVerbosityLevel;
            if (dotnetVerbosityLevel.ToLower().Equals("o"))
                dotnetVerbosityLevel = "q";

            string args = string.Format($"publish -v {dotnetVerbosityLevel} --source {sourceFullPath} --output {publishFullPath}");

            mLogger.LogInformation("Build and publish {projectName} to {publishFullPath} started", sourcePath, publishFullPath);

            try
            {
                mBuildLogs.Clear();
                DirectoryUtilites.CreateOrClearDirectory(publishFullPath);
                
                int exitCode = await Task.Run(() => StartProcessAsync(workingDirectory: sourceFullPath, arguments: args));

                if(exitCode != 0 && mDotnetVerbosityLevel.ToLower().Equals("o"))
                {
                    mLogger.LogError("Build {projectName} finished with eror code {exitCode}. The project build log will be shown below.", sourcePath, exitCode);

                    mBuildLogs.ForEach(message =>
                    {
                        mLogger.LogError("{buildLogMessage}", message);
                    });
                }
                    

                mLogger.LogInformation("Publish {projectName} finished with code {exitCode}", sourcePath, exitCode);
            }
            catch (Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }
        }

        private int StartProcessAsync(string command = "dotnet", string workingDirectory = "", string arguments = "")
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

            return process.ExitCode;
        }

        private void ProcessExited(object? sender, EventArgs e)
        {
            if(!mDotnetVerbosityLevel.ToLower().Equals("o"))
                mLogger.LogInformation("[DOTNET]: exited"); 
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var message = e.Data;

            if(!string.IsNullOrEmpty(message))
            {
                mBuildLogs.Add(message);

                if (!mDotnetVerbosityLevel.ToLower().Equals("o"))
                    mLogger.LogInformation("[DOTNET]: {data}", e.Data);
            }
        }

        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var message = e.Data;

            if (!string.IsNullOrEmpty(message))
            {
                mBuildLogs.Add(message);
                mLogger.LogError("[DOTNET]: {data}", e.Data);
            }
                
        }

        Task IDotnetService.PublishAsync()
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}
