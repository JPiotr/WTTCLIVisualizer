namespace WTTCLIVizualizer.Data;

public class Duration
    {
        public Guid id { get; set; }
        public required string state { get; set; }
        public long begin { get; set; }
        public long end { get; set; }
        public long duration { get; set; }
    }

