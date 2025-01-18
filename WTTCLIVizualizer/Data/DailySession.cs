
namespace WTTCLIVizualizer.Data;

    public class DailySession
    {
        public DateTime date { get; set; }
        public required List<Session> sessions { get; set; }
    }

