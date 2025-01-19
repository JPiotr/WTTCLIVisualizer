using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;
using WTTCLIVizualizer.Data;
using WTTCLIVizualizer.Logic;

namespace WTTCLIVizualizer.Commands;

public class InteractiveCommand : Command<InteractiveCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Lunch program witch interactive input.")]
        [CommandOption("-i|--interactive")]
        [DefaultValue(false)]
        public bool Ineractive { get; set; }

        [Description("For what user generate chart, no user specived means all avaliable users.")]
        [CommandOption("-u|--user <VALUES>")]
        public string? User { get; set; }

        [Description("Custom filename to analize.")]
        [CommandOption("-f|--filename <VALUE>")]
        [DefaultValue(".workingtime")]
        public string? FileName { get; set; }

        [Description("Start date of registred data.")]
        [CommandOption("-s|--startDate <VALUE>")]
        public string? StartDate
        {
            get
            {
                return Start.ToString();
            }
            set
            {
                if (DateOnly.TryParse(value, CultureInfo.CurrentCulture, out DateOnly parsedDate))
                {
                    Start = parsedDate;
                }
            }
        }
        public DateOnly Start = DateOnly.MinValue;
        [Description("End date of registred data.")]
        [CommandOption("-e|--endDate <VALUE>")]
        public string? EndDate
        {
            get
            {
                return End.ToString();
            }
            set
            {
                if (DateOnly.TryParse(value, CultureInfo.CurrentCulture, out DateOnly parsedDate))
                {
                    End = parsedDate;
                }
            }
        }
        public DateOnly End = DateOnly.MaxValue;

        [Description("Barchart")]
        [CommandOption("-b|--bar")]
        public bool? Bar { get; set; }
        [Description("Breakdown")]
        [CommandOption("-d|--breakdown")]
        public bool? Breakdown { get; set; }
        [Description("Table")]
        [CommandOption("-t|--table")]
        public bool? Table { get; set; }

    }
    Root? Deserialized { get; set; } = null;
    Arguments Arguments { get; set; } = new();
    Color[] Colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Violet, Color.Lime, Color.Maroon };
    public override int Execute(CommandContext context, Settings settings)
    {
        Dictionary<string, double> chartItemData = new();
        var chartItemColors = new Dictionary<string, Color>();
        Enm enumValues;
        List<Session> values;
        if (!settings.Ineractive)
        {
            if (settings.FileName != null)
            {
                Arguments.FileName = settings.FileName;
            }
            ProgressOutput();
            if (Arguments.Users.Count > 1 && (settings.User != null || settings.User != string.Empty))
            {
                if (Arguments.Users.Contains(settings.User))
                {
                    Arguments.Users = new List<string>() { settings.User };
                }
                else
                {
                    AnsiConsole.WriteException(new NoSuchUserException($"There is no such user ({settings.User}) in file!"));
                    return -1;
                }
            }
            var dates = GetDatesFromFIle();
            dates.Sort();
            enumValues = PrepereChartInput(chartItemData, chartItemColors);

            if (dates.Count() == 1)
            {
                Arguments.StartDate = Arguments.EndDate = dates.First();
            }
            else
            {
                Arguments.StartDate = settings.Start == DateOnly.MinValue && settings.End >= dates.First() ? dates.First() : settings.Start; 
                Arguments.EndDate = settings.End == DateOnly.MaxValue && settings.Start <= dates.Last() ? dates.Last() : settings.End; 
            }

            if (Arguments.StartDate == Arguments.EndDate)
            {
                AnsiConsole.Write(new Markup($"Loaded data for [yellow bold]{Arguments.StartDate}[/].{Environment.NewLine}"));
            }
            else
            {
                AnsiConsole.Write(new Markup($"Loaded data for period [yellow bold]{Arguments.StartDate}-{Arguments.EndDate}[/].{Environment.NewLine}"));
            }
            values = PrepereData();
            Calculate(chartItemData, values);
            if (settings.Bar.HasValue)
            {
                if (settings.Bar.Value)
                {
                    Arguments.Chart = ChartType.Bar;
                }
            }
            else if (settings.Breakdown.HasValue)
            {
                if (settings.Breakdown.Value)
                {
                    Arguments.Chart = ChartType.BreakDown;
                }
            }
            else if (settings.Table.HasValue)
            {
                if (settings.Table.Value)
                {
                    Arguments.Chart = ChartType.Table;
                }
            }
            RenderChart(chartItemData, chartItemColors, enumValues);

            return 0;
        }

        Arguments.FileName = AnsiConsole.Prompt(
            new TextPrompt<string>("File name: ").AllowEmpty().DefaultValue(Arguments.FileName)
        );
        ProgressOutput();
        if (Arguments.Users.Count == 1)
        {
            AnsiConsole.Write(new Markup($"Loaded [yellow bold]{Arguments.Users[0]}[/] user data.{Environment.NewLine}"));
        }
        else
        {
            SelectUsers();
        }
        RetriveDates();
        enumValues = PrepereChartInput(chartItemData, chartItemColors);
        values = PrepereData();
        Calculate(chartItemData, values);
        SelectChart();
        RenderChart(chartItemData, chartItemColors, enumValues);
        return 0;
    }

    private void ProgressOutput()
    {
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
            var task1 = ctx.AddTask("[green]Decrypting file[/]");
            var task2 = ctx.AddTask("[green]Looking users[/]");
            while (!ctx.IsFinished)
            {
                await DeserializeData();
                if (task1.IsFinished)
                {
                    task2.StartTask();
                    await RetriveUsers();
                    task2.Increment(1);
                }
                task1.Increment(1);
            }
        }).Wait();
    }

    private void RenderChart(Dictionary<string, double> chartItemData, Dictionary<string, Color> chartItemColors, Enm enumValues)
    {
        var rule = new Rule("[blue bold]Number of hours spend on action[/]");
        rule.Justification = Justify.Center;
        AnsiConsole.Write(rule);
        switch (Arguments.Chart)
        {
            case ChartType.Bar:
                AnsiConsole.Write(new BarChart()
                        // .Width(60)
                        .AddItems(chartItemData, (item) => new BarChartItem(
                            item.Key, Math.Round(item.Value / 1000 / 60 / 60, 2), chartItemColors[item.Key])));
                break;
            case ChartType.BreakDown:
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new BreakdownChart()
                        .Width(60)
                        .AddItems(chartItemData, (item) => new BreakdownChartItem(
                            item.Key, Math.Round(item.Value / 1000 / 60 / 60, 2), chartItemColors[item.Key])));
                break;
            case ChartType.Table:
                Table table = new Table();
                foreach (var item in chartItemData)
                {
                    table.AddColumn($"[{chartItemColors[item.Key].ToMarkup()}]{item.Key}[/]");
                }
                var sb = new List<string>();
                foreach (var item in enumValues.values)
                {
                    sb.Add($"[{chartItemColors[item].ToMarkup()}]{Math.Round(chartItemData[item] / 1000 / 60 / 60, 2)}[/]");
                }
                table.Alignment(Justify.Center);
                table.AddRow(sb.ToArray());
                AnsiConsole.Write(table);
                break;
        }
        AnsiConsole.WriteLine();
    }

    private void SelectChart()
    {
        Arguments.Chart = AnsiConsole.Prompt(
            new SelectionPrompt<ChartType>()
                .Title($"What type of chart do You want?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more dates)[/]")
                .AddChoices([ChartType.Bar, ChartType.BreakDown, ChartType.Table])
        );
    }

    private void Calculate(Dictionary<string, double> chartItemData, List<Session> values)
    {
        values.ForEach(x =>
        {
            if (x.actionType != "Idle" && x.actionType != "Stop")
            {
                chartItemData[x.actionType] += x.sessionInfo.duration;
                chartItemData["Idle"] += x.sessionInfo.idle - x.sessionInfo.duration;
            }
            if (x.actionType == "Idle")
            {
                chartItemData["Idle"] += x.sessionInfo.idle;
            }
        });
    }

    private Enm PrepereChartInput(Dictionary<string, double> chartItemData, Dictionary<string, Color> chartItemColors)
    {
        var enumValues = Deserialized.extConfig.enums.Where(x => x.name == "ActionType").Single();
        for (int i = 0; i < enumValues.values.Count; i++)
        {
            chartItemData.Add(enumValues.values[i], 0);
            chartItemColors.Add(enumValues.values[i], Colors[i]);
        }
        return enumValues;
    }

    private List<Session> PrepereData()
    {
        var values = Deserialized.data
                    .Where(userData => Arguments.Users.Contains(userData.user))
                    .SelectMany(userData => userData.dailySessions
                        .Where(daily =>
                            DateOnly.FromDateTime(daily.date) >= Arguments.StartDate &&
                            DateOnly.FromDateTime(daily.date) <= Arguments.EndDate)
                        .Select(daily => daily))
                    .SelectMany(session => session.sessions)
                    .ToList();
        return values;
    }

    private void RetriveDates()
    {
        List<DateOnly> dates = GetDatesFromFIle();
        dates.Sort();
        if (dates.Count == 1)
        {
            Arguments.StartDate = Arguments.EndDate = dates[0];
            AnsiConsole.Write(new Markup($"Loaded data for [yellow bold]{dates[0]}[/].{Environment.NewLine}"));
        }
        else
        {
            Arguments.StartDate = AnsiConsole.Prompt(
                new SelectionPrompt<DateOnly>()
                    .Title($"From which date u want analize?")
                    .EnableSearch()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
                    .AddChoices(dates)
            );
            Arguments.EndDate = AnsiConsole.Prompt(
                new SelectionPrompt<DateOnly>()
                    .Title($"To which date u want analize? [yellow bold]From = {Arguments.StartDate}[/]")
                    .EnableSearch()
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more dates)[/]")
                    .AddChoices(dates.Where(d => d > Arguments.StartDate).Append(Arguments.StartDate))
            );
            if (Arguments.StartDate == Arguments.EndDate)
            {
                AnsiConsole.Write(new Markup($"Loaded data for [yellow bold]{dates[0]}[/].{Environment.NewLine}"));
            }
            else
            {
                AnsiConsole.Write(new Markup($"Selected period [yellow bold]{Arguments.StartDate}-{Arguments.EndDate}[/].{Environment.NewLine}"));
            }
        }
    }

    private List<DateOnly> GetDatesFromFIle()
    {
        return Deserialized.data
            .AsParallel()
            .Where(userData => Arguments.Users.Contains(userData.user))
            .SelectMany(userData => userData.dailySessions.Select(daily => new DateOnly(daily.date.Year, daily.date.Month, daily.date.Day)))
            .Distinct()
            .ToList();
    }

    private void SelectUsers()
    {
        var choice = AnsiConsole.Prompt(
                        new MultiSelectionPrompt<string>()
                            .Title("Which user You want to check? (no choice means all)")
                            .NotRequired() // Not required to have a favorite fruit
                            .PageSize(10)
                            .MoreChoicesText("[grey](Move up and down to reveal more users)[/]")
                            .InstructionsText(
                                "[grey](Press [blue]<space>[/] to toggle, " +
                                "[green]<enter>[/] to accept)[/]")
                            .AddChoices(Arguments.Users));
        Arguments.Users = choice.Count != 0 ? choice : Arguments.Users;

        AnsiConsole.Write(new Markup($"Selected:{Environment.NewLine}"));
        foreach (string user in Arguments.Users)
        {
            AnsiConsole.Write(new Markup($"- [green bold]{user}[/]{Environment.NewLine}"));
        }
    }

    private async Task RetriveUsers()
    {
        Arguments.Users = Deserialized.data.Select(x => x.user).ToList();
    }

    private async Task DeserializeData()
    {
        Deserialized = await DataGetter.ReadAndDeserializeJsonAsync(Arguments.FileName);
        if (Deserialized == null) AnsiConsole.WriteException(new Exception("There are no data in file!"));
    }

}
