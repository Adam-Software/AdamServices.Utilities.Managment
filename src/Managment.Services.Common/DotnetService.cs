using Managment.Interface;
using Managment.Interface.Common;
using Managment.Interface.Common.JsonModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Managment.Services.Common
{
    public class DotnetService : IDotnetService
    {
        #region Services

        private readonly ILogger<DotnetService> mLogger;
        private readonly IJsonTempFilesWorkerService mTempFileWorkerService;

        #endregion

        #region Const

        #endregion

        #region Var

        private readonly string mDotnetVerbosityLevel = "q";
        private readonly List<string> mBuildLogs = [];
        private readonly string mBuildDirrectory;
        private readonly string mPublishDirrectory;

        #endregion

        #region ~

        public DotnetService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<DotnetService>>();
            mTempFileWorkerService = serviceProvider.GetService<IJsonTempFilesWorkerService>();

            IAppSettingsOptionsService appSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();
            
            mDotnetVerbosityLevel = appSettingsOptionsService.DotnetServiceSettings.DotnetVerbosityLevel;

            string buildDirrectory = Path.Combine(appSettingsOptionsService.WorkingDirrectory, CommonFilesAndDirectoriesNames.BuildDirrectory);
            mBuildDirrectory = new DirectoryInfo(buildDirrectory).FullName;

            string publishDirrectory = Path.Combine(appSettingsOptionsService.WorkingDirrectory, CommonFilesAndDirectoriesNames.PublishDirrectory);
            mPublishDirrectory = new DirectoryInfo(publishDirrectory).FullName;

            mLogger.LogInformation("=== DotnetService. Start ===");
        }

        #endregion

        #region Public methods

        public async Task PublishAsync(CancellationToken cancellationToken)
        {
            try
            {
                ServiceRepositories temp = await mTempFileWorkerService.ReadTempFileAsync();
                
                foreach (RepositoriesBaseInfo serviceRepository in temp.ServiceFilesContent)
                {
                    await StartDotnetPublishAsync(serviceRepository.RepositoriesName, cancellationToken);
                }
            }
            catch (Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                Dictionary<string, ServiceInfoModel> serviceInfos = await mTempFileWorkerService.ReadPublishedProjectFileInfoAsync();

                foreach (var serviceInfo in serviceInfos)
                {
                    string projectDirrectory = serviceInfo.Key;
                    string projectName = serviceInfo.Value.Services.Name;
                    string execArguments = serviceInfo.Value.ExecutionOptions.Arguments;

                    string execPath = Path.Combine(mPublishDirrectory, projectDirrectory, $"{projectName}.dll");
                    
                    _ = StartDotnetExecAsync(exeсPath: execPath, arguments: execArguments, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }
        }

        public void Dispose()
        {
            mLogger.LogInformation("=== DotnetService. Dispose ===");
        }

        #endregion

        #region Private methods

        private async Task StartDotnetExecAsync(string exeсPath, string arguments, CancellationToken cancellationToken)
        {
            string args = string.Format($"exec {exeсPath} {arguments}");
            string workingDirectory = Path.GetDirectoryName(exeсPath);
            string projectName = Path.GetFileNameWithoutExtension(exeсPath);

            mLogger.LogInformation("Run {projectName}", projectName);

            if (!string.IsNullOrEmpty(arguments)) 
                mLogger.LogInformation("Run with arguments {arguments}",  arguments);

            try
            {
                await StartProcess(arguments: args, workingDirectory: workingDirectory, cancellationToken: cancellationToken);
            }
            catch (TaskCanceledException)
            {
                mLogger.LogInformation("{projectName} was canceled", projectName);
            }
            catch (Exception ex)
            {
                mLogger.LogError("{error}", ex.Message);
            }
        }


        private async Task StartDotnetPublishAsync(string projectRepositoryName, CancellationToken cancellationToken)
        {
            string sourcePath = Path.Combine(mBuildDirrectory, projectRepositoryName);
            string publishPath = Path.Combine(mPublishDirrectory, projectRepositoryName);
            string dotnetVerbosityLevel = mDotnetVerbosityLevel;

            if (dotnetVerbosityLevel.ToLower().Equals("o"))
                dotnetVerbosityLevel = "q";

            string args = string.Format($"publish -v {dotnetVerbosityLevel} --source {projectRepositoryName} --output {publishPath}");

            mLogger.LogInformation("Build and publish {projectName} to {publishFullPath} started", projectRepositoryName, publishPath);

            try
            {
                mBuildLogs.Clear();
                DirectoryUtilites.CreateOrClearDirectory(publishPath);
                
                int exitCode = await StartProcess(workingDirectory: sourcePath, arguments: args, cancellationToken: cancellationToken);

                if(exitCode != 0 && mDotnetVerbosityLevel.ToLower().Equals("o"))
                {
                    mLogger.LogError("Build {projectName} finished with eror code {exitCode}. The project build log will be shown below.", sourcePath, exitCode);

                    mBuildLogs.ForEach(message =>
                    {
                        mLogger.LogError("{buildLogMessage}", message);
                    });
                }

                if (exitCode == 0) 
                {
                    File.Copy(Path.Combine(sourcePath, "service_info.json"), Path.Combine(publishPath, "service_info.json"), true);
                }

                mLogger.LogInformation("Publish {projectName} finished with code {exitCode}", projectRepositoryName, exitCode);
            }
            catch (Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }
        }

        
        private async Task<int> StartProcess(string command = "dotnet", string workingDirectory = "", string arguments = "", CancellationToken cancellationToken = default)
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

            cancellationToken.Register(process.Kill);

            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode;
        }

        private void ProcessExited(object sender, EventArgs e)
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

        #endregion
    }
}
