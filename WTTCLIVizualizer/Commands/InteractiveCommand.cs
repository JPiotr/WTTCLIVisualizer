using System;
using System.Runtime.InteropServices;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;
using WTTCLIVizualizer.Data;
using WTTCLIVizualizer.Logic;

namespace WTTCLIVizualizer.Commands;

public class InteractiveCommand : Command<InteractiveCommand.Settings>
{
    Color[] Colors = new Color[]{Color.Red,Color.Green,Color.Blue,Color.Yellow,Color.Violet,Color.Lime,Color.Maroon};
    public override int Execute(CommandContext context, Settings settings)
    {
        Arguments arguments = new Arguments();
        bool confirmInteractive = AnsiConsole.Prompt(
            new TextPrompt<bool>("Do you want interactive usage?")
            .AddChoice(true)
            .AddChoice(false)
            .DefaultValue(true)
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
            DateOnly fromDate = default;
            DateOnly toDate = default;
            if(dates.Count == 1){
                fromDate = toDate = dates[0];
                AnsiConsole.Write(new Markup($"Loaded data for [yellow bold]{dates[0]}[/].{Environment.NewLine}"));
            }
            else{
                fromDate = AnsiConsole.Prompt(
                    new SelectionPrompt<DateOnly>()
                        .Title($"From which date u want analize?")
                        .EnableSearch()
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
                        .AddChoices(dates)
                );
                toDate = AnsiConsole.Prompt(
                    new SelectionPrompt<DateOnly>()
                        .Title($"To which date u want analize? [yellow bold]From = {fromDate}[/]")
                        .EnableSearch()
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more dates)[/]")
                        .AddChoices(dates.Where(d=>d > fromDate).Append(fromDate))
                );
                if(fromDate == toDate){
                    AnsiConsole.Write(new Markup($"Loaded data for [yellow bold]{dates[0]}[/].{Environment.NewLine}"));
                }else{
                    AnsiConsole.Write(new Markup($"Selected period [yellow bold]{fromDate}-{toDate}[/].{Environment.NewLine}"));
                }
            }
        Dictionary<string,double> chartItemData = new();
        var chartItemColors = new Dictionary<string,Color>();
        var enumValues = deserialized.extConfig.enums.Where(x=>x.name == "ActionType").Single();
        for(int i=0;i<enumValues.values.Count;i++){
            chartItemData.Add(enumValues.values[i],0);
            chartItemColors.Add(enumValues.values[i],Colors[i]);
        }
        //getting type of chart
        var chartType = AnsiConsole.Prompt<ChartType>(
            new SelectionPrompt<ChartType>()
                .Title($"What type of chart do You want?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more dates)[/]")
                .AddChoices(new []{ChartType.Bar,ChartType.BreakDown,ChartType.Table})
        );
        var values = deserialized.data
            .AsParallel() // Enable parallel processing
            .Where(userData => avaliableUsers.Contains(userData.user)) 
            .SelectMany(userData => userData.dailySessions
                .Where(daily=>
                    new DateOnly(daily.date.Year, daily.date.Month, daily.date.Day) == fromDate 
                    || new DateOnly(daily.date.Year, daily.date.Month, daily.date.Day) == toDate
                    ).Select(daily=>daily))
            .SelectMany(session=>session.sessions)
            .ToList();

        values.ForEach(x=>{
            if(x.actionType != "Idle"){
                chartItemData[x.actionType] += x.sessionInfo.duration;
            }
            if(x.actionType == "Idle"){
                chartItemData["Idle"] += x.sessionInfo.idle;
            }
        });
        var rule = new Rule("[blue bold]Number of hours spend on action[/]");
        rule.Justification = Justify.Center;
        AnsiConsole.Write(rule);
        switch(chartType){
            case ChartType.Bar:
                AnsiConsole.Write(new BarChart()
                        // .Width(60)
                        .AddItems(chartItemData, (item) => new BarChartItem(
                            item.Key, Math.Round(item.Value/1000/60/60,2) , chartItemColors[item.Key])));
            break;
            case ChartType.BreakDown:
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new BreakdownChart()
                        .Width(60)
                        .AddItems(chartItemData, (item) => new BreakdownChartItem(
                            item.Key, Math.Round(item.Value/1000/60/60,2) , chartItemColors[item.Key])));
            break;
            case ChartType.Table:
                Table table = new Table();
                foreach(var item in chartItemData){
                    table.AddColumn($"[{chartItemColors[item.Key].ToMarkup()}]{item.Key}[/]");
                }
                var sb = new List<string>();
                foreach(var item in enumValues.values){
                    sb.Add($"[{chartItemColors[item].ToMarkup()}]{Math.Round(chartItemData[item]/1000/60/60,2)}[/]");
                }
                table.Alignment(Justify.Center);
                table.AddRow(sb.ToArray());
                AnsiConsole.Write(table); 
            break;
        }
        AnsiConsole.WriteLine();
        return 0;
    }

    public class Settings : CommandSettings{
        
    }
}
