namespace RWS.PlanningPoker.Server.Models;

public record CreateRoomRequest(string RoomName, string ManagerName);
public record VoteRequest(string Name, string Value);