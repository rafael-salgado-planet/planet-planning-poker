using System.Collections.Concurrent;
using System.Threading;

namespace RWS.PlanningPoker.Server.Services;

public class RoomStore
{
    private readonly ConcurrentDictionary<Guid, Room> _rooms = new();
    private static int _instanceCount = 0;
    private readonly int _instanceId;

    public RoomStore()
    {
        _instanceId = Interlocked.Increment(ref _instanceCount);
        Console.WriteLine($"[RoomStore] Instance #{_instanceId} created. Total instances: {_instanceCount}");
    }

    public Room Create(string name, string managerName, string? managerId = null)
    {
        var room = new Room { Id = Guid.NewGuid(), Name = name, ManagerName = managerName };
        room.Join(managerName, managerId ?? Guid.NewGuid().ToString());
        _rooms[room.Id] = room;
        
        Console.WriteLine($"[RoomStore #{_instanceId}] Created room {room.Id} with name '{name}' and manager '{managerName}'. Total rooms in this instance: {_rooms.Count}");
        
        return room;
    }

    public Room? Get(Guid id)
    {
        var exists = _rooms.TryGetValue(id, out var room);
        Console.WriteLine($"[RoomStore #{_instanceId}] Get room {id}: {(exists ? "FOUND" : "NOT FOUND")}. Total rooms in this instance: {_rooms.Count}");
        
        if (!exists && _rooms.Count > 0)
        {
            Console.WriteLine($"[RoomStore #{_instanceId}] Available room IDs: {string.Join(", ", _rooms.Keys)}");
        }
        
        return room;
    }

    public List<Room> GetAllRooms()
    {
        return _rooms.Values.ToList();
    }

    public void RemoveUserFromRoom(Guid roomId, string userName)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            var isEmpty = room.Leave(userName);
            if (isEmpty)
            {
                _rooms.TryRemove(roomId, out _);
                Console.WriteLine($"[RoomStore #{_instanceId}] Removed empty room {roomId}");
            }
        }
    }
}

public class Room
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public bool IsVotingOpen { get; set; }
    public HashSet<string> Participants { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Votes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> UserIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Join(string name, string userId)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(userId))
            return false;

        var userName = name.Trim();
        var id = userId.Trim();

        if (Participants.Contains(userName))
        {
            if (UserIds.TryGetValue(userName, out var existingId))
            {
                return string.Equals(existingId, id, StringComparison.Ordinal);
            }
            return false;
        }

        Participants.Add(userName);
        UserIds[userName] = id;
        return true;
    }

    public bool Leave(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var trimmed = name.Trim();
        Participants.Remove(trimmed);
        Votes.Remove(trimmed);
        UserIds.Remove(trimmed);

        if (string.Equals(ManagerName, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            ManagerName = Participants.FirstOrDefault() ?? string.Empty;
        }

        return Participants.Count == 0;
    }

    public void StartVoting()
    {
        IsVotingOpen = true;
        Votes.Clear();
    }
    public void FinishVoting()
    {
        IsVotingOpen = false;
    }
    public void CastVote(string name, string value, string? userId = null)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || !IsVotingOpen)
            return;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var _ = Join(trimmed, userId);
        }
        else
        {
            Participants.Add(trimmed);
        }
        Votes[trimmed] = value;
    }

    public RoomDto ToDto()
    {
        var (avg, tally) = GetResults();
        return new RoomDto
        {
            Id = Id,
            Name = Name,
            ManagerName = ManagerName,
            IsVotingOpen = IsVotingOpen,
            Participants = Participants.ToList(),
            Votes = Votes.ToDictionary(k => k.Key, v => v.Value),
            Average = avg,
            Tally = tally
        };
    }

    public (double? average, Dictionary<string,int> tally) GetResults()
    {
        var tally = Votes.Values
            .GroupBy(v => v)
            .ToDictionary(g => g.Key, g => g.Count());

        var numeric = Votes.Values
            .Select(v => int.TryParse(v, out var n) ? n : (int?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .ToList();

        double? avg = numeric.Count > 0 ? numeric.Average() : null;
        return (avg, tally);
    }
}

public class RoomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public bool IsVotingOpen { get; set; }
    public List<string> Participants { get; set; } = [];
    public Dictionary<string,string> Votes { get; set; } = [];
    public double? Average { get; set; }
    public Dictionary<string,int> Tally { get; set; } = [];
}