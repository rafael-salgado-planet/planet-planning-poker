# planet-planning-poker
A simple Blazor Server + SignalR Planning Poker app to run lightweight estimation sessions in real time.
# RWS.PlanningPoker

A lightweight **Planning Poker** app built with **.NET 10**, **Blazor Server**, and **SignalR** for real-time estimation sessions.

MS Copilot-oriented development

## Features

- Create a room and share the URL
- Join rooms from any browser
- Real-time participant updates using SignalR
- Start/finish voting (room manager)
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
