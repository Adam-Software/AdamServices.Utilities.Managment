using CommandLine;
using Managment.Interface;

namespace Managment.Core
{
    public class AppArguments : IAppArguments
    {

        [Option('u', "update", Required = false, HelpText = "Update mode. Update install services")]
        public bool Update { get; set; }

        [Option('i', "install", Required = false, HelpText = "Install mode. Install services")]
        public bool Install { get; set; }
    }
}
