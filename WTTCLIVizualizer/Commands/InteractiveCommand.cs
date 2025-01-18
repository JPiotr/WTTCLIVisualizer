using System;
using System.Runtime.InteropServices;
using Spectre.Console;
using Spectre.Console.Cli;
using WTTCLIVizualizer.Data;
using WTTCLIVizualizer.Logic;

namespace WTTCLIVizualizer.Commands;

public class InteractiveCommand : Command<InteractiveCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        Arguments arguments = new Arguments();
        bool confirmInteractive = AnsiConsole.Prompt(
            new TextPrompt<bool>("Do you want interactive usage?")
            .AddChoice(true)
            .AddChoice(false)
            .DefaultValue(false)
            .WithConverter(choice => choice ? "y" : "n")
        );
        if(!confirmInteractive){
            AnsiConsole.Write(new Markup($"Then try run this app with [yellow bold]-h[/] option{Environment.NewLine}"));
            return -1;
        } 
        arguments.FileName = AnsiConsole.Prompt(
            new TextPrompt<string>("File name: ").AllowEmpty().DefaultValue(arguments.FileName)
        );
        Root? deserialized = null;
        AnsiConsole.Progress()
        .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn(),
            ])
        .StartAsync(async ctx =>
        {
            // Define tasks
            var task1 = ctx.AddTask("[green]Reading file[/]");
            while (!ctx.IsFinished)
            {
                deserialized = await DataGetter.ReadAndDeserializeJsonAsync(arguments.FileName);
                task1.Increment(1);
            }
        }).Wait();
        if(deserialized == null) AnsiConsole.WriteException(new Exception("There are no data in file!"));

        List<string> avaliableUsers = deserialized.data.Select(x=>x.user).ToList();
        if(avaliableUsers.Count == 1){
            AnsiConsole.Write(new Markup($"Loaded [yellow bold]{avaliableUsers[0]}[/] user data.{Environment.NewLine}"));
        }else{
            var choice = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Which user You want to check? (no choice means all)")
                    .NotRequired() // Not required to have a favorite fruit
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more users)[/]")
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle, " + 
                        "[green]<enter>[/] to accept)[/]")
                    .AddChoices(avaliableUsers));
                avaliableUsers = choice.Count != 0 ? choice : avaliableUsers;

            // Write the selected fruits to the terminal
            AnsiConsole.Write(new Markup($"Selected:{Environment.NewLine}"));
            foreach (string user in avaliableUsers)
            {
                AnsiConsole.Write(new Markup($"- [green bold]{user}[/]{Environment.NewLine}"));
            }
        }
        List<DateOnly> dates = deserialized.data
            .AsParallel() // Enable parallel processing
            .Where(userData => avaliableUsers.Contains(userData.user)) 
            .SelectMany(userData => userData.dailySessions.Select(daily => new DateOnly(daily.date.Year, daily.date.Month, daily.date.Day)))
            .Distinct() // Remove duplicate dates
            .ToList();
            if(dates.Count == 1){
                AnsiConsole.Write(new Markup($"Loaded data for [yellow bold]{dates[0]}[/].{Environment.NewLine}"));
            }
            else{
                var fromDate = AnsiConsole.Prompt(
                    new SelectionPrompt<DateOnly>()
                        .Title($"From which date u want analize?")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
                        .AddChoices(dates)
                );
                var toDate = AnsiConsole.Prompt(
                    new SelectionPrompt<DateOnly>()
                        .Title($"From which date u want analize?")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more dates)[/]")
                        .AddChoices(dates.Where(d=>d > fromDate))
                );
                AnsiConsole.Write(new Markup($"Selected data [yellow bold]{fromDate}-{toDate}[/].{Environment.NewLine}"));
            }
        Dictionary<string,long> chartItemData = new();
        var enumValues = deserialized.extConfig.enums.Where(x=>x.name == "ActionType").Single();
        foreach(string enm in enumValues.values){
            
        }
        return 0;
    }

    public class Settings : CommandSettings{
        
    }
}
