namespace WTTCLIVizualizer.Data;

public class Data
    {
        public bool config { get; set; }
        public required string user { get; set; }
        public required List<DailySession> dailySessions { get; set; }
    }

