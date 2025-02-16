using Spectre.Console;
using Spectre.Console.Cli;

namespace Managment.Core.SpectreConsole.Commands
{
    public sealed class DefaultCommand: Command
    {
        private readonly IAnsiConsole mConsole;

        public DefaultCommand(IAnsiConsole console)
        {
            mConsole = console;
        }

        public override int Execute(CommandContext context)
        {
            
            //mConsole.MarkupLine("[yellow]Hello[/] [blue]World[/]!");
            return 0;
        }
    }
}
