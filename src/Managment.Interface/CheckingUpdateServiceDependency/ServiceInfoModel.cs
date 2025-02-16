using System.Collections.Generic;

namespace Managment.Interface.CheckingUpdateServiceDependency
{
    public class ServiceInfoModel
    {
        public ServiceSection Services { get; set; }
        public CompilerOptions CompilerOptions { get; set; }
        public Systemd Systemd { get; set; }
    }

    public class ServiceSection
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public List<string> Dependencies { get; set; }
    }

    public class CompilerOptions
    {
        public string ServiceProjectPath { get; set; }
        public string ServiceOutputPath { get; set; }
        public string ServiceBuildPath { get; set; }
    }

    public class Systemd
    {
        public string ServiceFilePath { get; set; }
    }
}
