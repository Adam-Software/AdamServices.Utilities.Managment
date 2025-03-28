using System.Collections.Generic;

namespace Managment.Interface.Common.JsonModels
{
    public class ServiceInfoModel
    {
        public ServiceSection Services { get; set; } = new();
        public ExecutionOptionsSection ExecutionOptions { get; set; } = new();
        public CompilerOptions CompilerOptions { get; set; } = new();
        public Systemd Systemd { get; set; } = new();
    }

    public class ServiceSection
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public ProjectType ProjectType { get; set; } = ProjectType.None;
        public List<string> Dependencies { get; set; } = [];   
    }

    public class ExecutionOptionsSection
    {
        public string Arguments { get; set; } = string.Empty;
    }

    public class CompilerOptions
    {
        public string ServiceProjectPath { get; set; } = string.Empty;
        public string ServiceOutputPath { get; set; } = string.Empty;
        public string ServiceBuildPath { get; set; } = string.Empty;
    }

    public class Systemd
    {
        public string ServiceFilePath { get; set; } = string.Empty;
    }

    public enum ProjectType
    {
        None = 0,
        DotnetProject = 1,
        PythonProject = 2
    }
}
