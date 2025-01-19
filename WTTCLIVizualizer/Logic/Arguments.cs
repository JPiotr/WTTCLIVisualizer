namespace WTTCLIVizualizer;

public class Arguments
{
    public string FileName { get; set; } = ".workingtime";
    public List<string> Users { get; set; } = new() { };
    public DateOnly StartDate { get; set; } = default;
    public DateOnly EndDate { get; set; } = default;
    public ChartType Chart { get; set; } = ChartType.BreakDown;
}