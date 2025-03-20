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

        #endregion

        #region Const

        

        #endregion

        #region Var

        private readonly string mSourceBuildPath = "build";
        private readonly string mPublishPath = "publish";
        private readonly string mDotnetVerbosityLevel = "q";
        private readonly List<string> mBuildLogs = [];
        private readonly List<string> mSuccessBuildExecPaths = [];
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

        public async Task PublishAsync(CancellationToken cancellationToken)
        {
            try
            {
                List<string> sourcePaths = await JsonUtilites.ReadJsonFileAsync<List<string>>(mSourceBuildPath, ServiceFileNames.BuildTargetPathsFileName);

                foreach (string sourcePath in sourcePaths)
                {
                    await StartDotnetPublishAsync(sourcePath, cancellationToken);
                }
            }
            catch (Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                List<string> execPaths = JsonUtilites.ReadJsonFileAsync<List<string>>(mPublishPath, ServiceFileNames.ServiceExecPathsFileName).Result;

                foreach (string execPath in execPaths)
                {
                    _ = StartDotnetExecAsync(exeсPath: execPath, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            mLogger.LogInformation("=== DotnetService. Dispose ===");
        }

        #endregion

        #region Private methods

        private async Task StartDotnetExecAsync(string exeсPath, CancellationToken cancellationToken)
        {
            string args = string.Format($"exec {exeсPath}");
            string workingDirectory = Path.GetDirectoryName(exeсPath);

            string projectName = Path.GetFileNameWithoutExtension(exeсPath);
            mLogger.LogInformation("Run {projectName}", projectName);

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


        private async Task StartDotnetPublishAsync(string sourcePath, CancellationToken cancellationToken)
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
                
                int exitCode = await StartProcess(workingDirectory: sourceFullPath, arguments: args, cancellationToken: cancellationToken);

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
                    File.Copy(Path.Combine(sourceFullPath, "service_info.json"), Path.Combine(publishFullPath, "service_info.json"), true);

                    var projectName = JsonUtilites.ReadJsonFileAsync<ServiceInfoModel>(sourceFullPath, "service_info.json").Result.Services.Name;

                    if (!string.IsNullOrEmpty(projectName)) 
                    {
                        string execProjectName = $"{projectName}.dll";
                        string serviceExecPath = Path.Combine(publishFullPath, execProjectName);
                        mSuccessBuildExecPaths.Add(serviceExecPath);
                    }   
                }

                if(mSuccessBuildExecPaths.Count > 0) 
                    await JsonUtilites.SerializeAndSaveJsonFilesAsync(mSuccessBuildExecPaths, mPublishPath, ServiceFileNames.ServiceExecPathsFileName);
                
                mLogger.LogInformation("Publish {projectName} finished with code {exitCode}", sourcePath, exitCode);
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

            cancellationToken.Register(() => 
            {
                process.Kill();
            });

            
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
