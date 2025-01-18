namespace WTTCLIVizualizer;

using Spectre.Console.Cli;
using WTTCLIVizualizer.Commands;

class Program
{
    static void Main(string[] args)
    {
        var app = new CommandApp<InteractiveCommand>();
        app.Configure(config =>
        {
#if DEBUG
            config.PropagateExceptions();
            config.ValidateExamples();
#endif
        });

        app.Run(args);
    }
}
