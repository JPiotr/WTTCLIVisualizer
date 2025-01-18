namespace WTTCLIVizualizer;

public class Arguments
{
    public string FileName { get; set; } = ".workingtime";
    public string UserName { get; set; } = string.Empty;
    public DateTime StarDate { get; set; } = DateTime.MinValue;
    public DateTime EndDate { get; set; } = DateTime.MaxValue;
    public List<string> Actions { get; set; } = new() { };
    public ChartType Chart { get; set; } = ChartType.BreakDown;
}