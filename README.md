# planet-planning-poker
A simple Blazor Server + SignalR Planning Poker app to run lightweight estimation sessions
# RWS.PlanningPoker

A lightweight **Planning Poker** app built with **.NET 10**, **Blazor Server**, and **SignalR** for real-time estimation sessions.

Ready for use at: http://q-psw01-de.qaplanetswitch.com/PlanetPoker/ (VPN Needed) 

MS Copilot-oriented development

## Features

- Create a room and share the URL
- Join rooms from any browser
- Real-time participant updates using SignalR
- Start/finish voting (room manager)
- No limit of users per room!
- Cast votes during an active round
- Results view with average + vote distribution charts
- Cookie-based identity (username + stable id)

## Tech Stack

- .NET 10
- Blazor Server (Interactive Server)
- SignalR
- BlazorBootstrap

## Run locally

```bash
dotnet run --project src/RWS.PlanningPoker/RWS.PlanningPoker/RWS.PlanningPoker.csproj
```

Then open the printed URL.

## Notes

- Room data is currently stored in-memory (no database). Restarting the app clears rooms.
- Identity is stored in an HttpOnly cookie: `planningpoker_UserIdentity`.
- IIS deployment: create a non-managed AppPool, enable the WebSocket protocol in the Windows features, install the ASP.NET hosting bundle, and URL rewrite 2.0. 
