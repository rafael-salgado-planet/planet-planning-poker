using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RWS.PlanningPoker.Server.Services;

namespace RWS.PlanningPoker.Server.Hubs;

public class RoomHub : Hub
{
    private readonly RoomStore _store;
    private readonly IUserService _userService;
    private static readonly Dictionary<string, (Guid roomId, string name)> _connections = new();
    private static readonly object _lock = new();
    public RoomHub(RoomStore store, IUserService userService)
    {
        _store = store;
        _userService = userService;
    }
    public async Task JoinRoom(Guid roomId, UserCookieRecord userInfo)
    {
        var room = _store.Get(roomId);
        if (room == null) return;

        if (string.IsNullOrWhiteSpace(userInfo?.Username))
        {
            await Clients.Caller.SendAsync("JoinRejected", "Name is required.");
            return;
        }

        // If name already in room with different id, reject so UI can redirect to join
        if (room.UserIds.TryGetValue(userInfo.Username, out var existingId) && !string.Equals(existingId, userInfo.Id, StringComparison.Ordinal))
        {
            await Clients.Caller.SendAsync("JoinRejected", "This room already has someone with that name. Please pick another.");
            return;
        }

        lock (_lock)
        {
            _connections[Context.ConnectionId] = (roomId, userInfo.Username);
        }

        if (!room.Participants.Contains(userInfo.Username))
        {
            var joined = room.Join(userInfo.Username, userInfo.Id);
            if (!joined)
            {
                await Clients.Caller.SendAsync("JoinRejected", "Username already in use in this room. Please choose another.");
                return;
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
        await Clients.All.SendAsync("Update");
    }

    public async Task LeaveRoom(Guid roomId)
    {
        (Guid roomId, string name)? info = null;
        lock (_lock)
        {
            if (_connections.TryGetValue(Context.ConnectionId, out var data))
            {
                info = data;
                _connections.Remove(Context.ConnectionId);
            }
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());

        if (info.HasValue)
        {
            // Remove user only from this room
            _store.RemoveUserFromRoom(info.Value.roomId, info.Value.name);
            await Clients.All.SendAsync("Update");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        (Guid roomId, string name)? info = null;
        lock (_lock)
        {
            if (_connections.TryGetValue(Context.ConnectionId, out var data))
            {
                info = data;
                _connections.Remove(Context.ConnectionId);
            }
        }

        if (info.HasValue)
        {
            // Remove user only from this room
            _store.RemoveUserFromRoom(info.Value.roomId, info.Value.name);
            await Clients.All.SendAsync("Update");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartVoting(Guid roomId)
    {
        var room = _store.Get(roomId);
        if (room != null)
        {
            room.StartVoting();
            await Clients.Group(roomId.ToString()).SendAsync("Update");
        }
    }

    public async Task FinishVoting(Guid roomId)
    {
        var room = _store.Get(roomId);
        if (room != null)
        {
            room.FinishVoting();
            await Clients.Group(roomId.ToString()).SendAsync("Update");
        }
    }

    public async Task CastVote(Guid roomId, string name, string value)
    {
        var room = _store.Get(roomId);
        if (room != null)
        {

          
            if (string.IsNullOrWhiteSpace(name)) return;

            room.CastVote(name, value);
            await Clients.Group(roomId.ToString()).SendAsync("Update");
        }
    }

    private class UserCookie
    {
        public string Username { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
    }
}