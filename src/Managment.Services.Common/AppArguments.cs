using CommandLine;
using Managment.Interface;

namespace Managment.Services.Common
{

    public class AppArguments : IAppArguments
    {

        [Option(shortName:'u', longName: "update", Required = false, HelpText = "Update mode. Update installed services")]
        public bool Update { get; set; }

        [Option(shortName: 'i', longName: "install", Required = false, HelpText = "Install mode. Install services")]
        public bool Install { get; set; }

        [Option(shortName: 'r', longName: "run", Required = false, HelpText = "Run mode. Launches previously downloaded services")]
        public bool Run { get ; set ; }

        public bool ValidateParameters()
        {
            int validateCount = 0;

            if (Update) validateCount++;
            if (Install) validateCount++;
            if (Run) validateCount++;

            return validateCount == 1;
        }
    }
}
