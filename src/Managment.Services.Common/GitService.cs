using Managment.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;


namespace Managment.Services.Common
{
    public class GitService : IGitService
    {
        private readonly ILogger<GitService> mLogger;

        private readonly string mGitPath = "C:\\Program Files\\Git\\bin\\git.exe";
        private readonly string mGitWorkngDirrectory = "sourceRepository";

        public GitService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<GitService>>();
        }


        public void Clone(string repositoryUrl)
        {
            StartProcess($"clone -v {repositoryUrl}");   
        }

        List<string> mBuffer = [];
        private async void StartProcess(string args)
        {
            mBuffer.Clear();
            string arg = string.Format($"{args}");

            ProcessStartInfo proccesInfo = new(mGitPath)
            {
                WorkingDirectory = mGitWorkngDirrectory,
                Arguments = arg,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                //RedirectStandardError = true
            };


            using Process process = new()
            {
                StartInfo = proccesInfo,     
            };

            process.OutputDataReceived += (s, e) => OnWriteOutputData("Output", e.Data);
            //process.ErrorDataReceived += (s, e) => OnWriteOutputData("Error", e.Data);

            process.Start();

            //string output = process.StandardOutput.ReadToEnd();
            //string error = process.StandardError.ReadToEnd();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            //process.WaitForInputIdle();
            process.WaitForExit();
           

            //mLogger.LogInformation("{data}", output);

            //if (!string.IsNullOrEmpty(error))
            //{
            //    mLogger.LogWarning("error {data}", error);
            //}
        }

        private void OnWriteOutputData(string type, string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            if(type == "Output")
                mLogger.LogInformation(data);

            if (type == "Error")
                mLogger.LogWarning(data);

        }
    }
}
