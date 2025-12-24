using System;
using Microsoft.AspNetCore.Mvc;
using RWS.PlanningPoker.Server.Services;
using RWS.PlanningPoker.Server.Models;

namespace RWS.PlanningPoker.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly RoomStore _store;
    private readonly IUserService _userService;

    public RoomsController(RoomStore store, IUserService userService)
    {
        _store = store;
        _userService = userService;
    }

    [HttpGet("{id:guid}/enter")]
    public IActionResult EnterRoomViaGet(Guid id)
    {
        var room = _store.Get(id);
        if (room is null) return NotFound("Room not found");
        
        return Redirect($"{Request.PathBase}/room/{id}");
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetRoom(Guid id)
    {
        Console.WriteLine($"[API] GetRoom called with id: {id}");
        var room = _store.Get(id);
        if (room == null)
        {
            Console.WriteLine($"[API] Room {id} not found");
            return NotFound();
        }
        Console.WriteLine($"[API] Room {id} found: {room.Name}");
        return Ok(room.ToDto());
    }

    [HttpPost("{id:guid}/start")]
    public IActionResult StartVoting(Guid id)
    {
        var room = _store.Get(id);
        if (room is null) return NotFound();
        room.StartVoting();
        return Ok(room.ToDto());
    }

    [HttpPost("{id:guid}/finish")]
    public IActionResult FinishVoting(Guid id)
    {
        var room = _store.Get(id);
        if (room is null) return NotFound();
        room.FinishVoting();
        return Ok(room.ToDto());
    }

    [HttpPost("{id:guid}/vote")]
    public IActionResult CastVote(Guid id, [FromBody] VoteRequest req)
    {
         var room = _store.Get(id);
         if (room is null) return NotFound();
        room.CastVote(req.Name, req.Value);
         return Ok(room.ToDto());
    }

    [HttpPost("createform")]
    [IgnoreAntiforgeryToken]
    public IActionResult CreateForm([FromForm] string roomName, [FromForm] string yourName)
    {
         try
         {
             if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(yourName))
             {
                 return BadRequest("Room name and your name are required");
             }

            _userService.SetCurrentUser(yourName.Trim());
            var info = _userService.GetCurrentUserInfo();
            var room = _store.Create(roomName.Trim(), yourName.Trim(), info.Id ?? Guid.NewGuid().ToString());
            
            Console.WriteLine($"[CreateForm] Created room {room.Id} for {yourName}");
            
            var verifyRoom = _store.Get(room.Id);
            Console.WriteLine($"[CreateForm] Verification: Room {room.Id} {(verifyRoom != null ? "EXISTS" : "DOES NOT EXIST")} in store");
            
            Console.WriteLine($"[CreateForm] Redirecting to {Request.PathBase}/room/{room.Id}");
            
            return Redirect($"{Request.PathBase}/room/{room.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateForm] Error creating room: {ex.Message}");
            Console.WriteLine($"[CreateForm] Stack trace: {ex.StackTrace}");
            return StatusCode(500, $"Error creating room: {ex.Message}");
        }
    }

    [HttpPost("joinform")]
    [IgnoreAntiforgeryToken]
    public IActionResult JoinForm([FromForm] Guid roomId, [FromForm] string yourName)
    {
        var room = _store.Get(roomId);
        if (room is null) return NotFound("Room not found");
        _userService.SetCurrentUser(yourName.Trim());
        return Redirect($"{Request.PathBase}/room/{roomId}");
    }

    [HttpPost("enterroom")]
    [IgnoreAntiforgeryToken]
    public IActionResult EnterRoom([FromForm] Guid roomId, [FromForm] string name)
    {
        var redirectBase = $"{Request.PathBase}/room/{roomId}";
        var trimmedName = name?.Trim();

        var room = _store.Get(roomId);
        if (room is null)
        {
            return Redirect($"{redirectBase}?joinError={Uri.EscapeDataString("Room not found.")}");
        }

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return Redirect($"{redirectBase}?joinError={Uri.EscapeDataString("Name is required.")}");
        }

        if (room.Participants.Contains(trimmedName))
        {
            return Redirect($"{redirectBase}?joinError={Uri.EscapeDataString("That name is already in use in this room. Please choose another.")}");
        }

        _userService.SetCurrentUser(trimmedName);
        return Redirect(redirectBase);
    }

    [HttpPost("create")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateRoomRequest req)
    {
        var info = _userService.GetCurrentUserInfo();
        var room = _store.Create(req.RoomName, req.ManagerName, info.Id ?? Guid.NewGuid().ToString());
         return Ok(new { id = room.Id, name = room.Name, manager = room.ManagerName });
    }

    [HttpGet("debug/rooms")]
    public IActionResult GetAllRooms()
    {
        // Debug endpoint to see all rooms
        var allRooms = new List<object>();
        // Since we can't easily enumerate ConcurrentDictionary, let's add a method to RoomStore
        var rooms = _store.GetAllRooms();
        return Ok(new { 
            message = $"Found {rooms.Count} rooms", 
            rooms = rooms.Select(r => new { r.Id, r.Name, r.ManagerName, Participants = r.Participants.Count }).ToList()
        });
    }
}
