namespace RWS.PlanningPoker.Components.Pages;

public partial class Room
{
    public class RoomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public bool IsVotingOpen { get; set; }
        public List<string> Participants { get; set; } = [];
        public Dictionary<string, string> Votes { get; set; } = [];
        public double? Average { get; set; }
        public Dictionary<string, int> Tally { get; set; } = [];
    }
}
