namespace WTTCLIVizualizer.Data;

public class SessionInfo
    {
        public Guid id { get; set; }
        public long duration { get; set; }
        public long idle { get; set; }
        public required string state { get; set; }
        public required List<Duration> durations { get; set; }
        public required List<string> filesExt { get; set; }
    }

