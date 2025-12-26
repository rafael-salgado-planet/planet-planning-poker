using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.WebUtilities;
using RWS.PlanningPoker.Server.Services;

namespace RWS.PlanningPoker.Components.Pages;

public partial class Room : ComponentBase, IDisposable
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private StateContainer StateContainer { get; set; } = default!;

    [Parameter] public Guid RoomId { get; set; }
    private RoomDto? room;
    private UserCookieRecord userInfo;
   
    private HubConnection? hubConnection;
   
    private bool _disposed;
    private bool showResultsModal;
    private string? joinErrorMessage;
    private Modal? resultsModal;
    private PieChart? pieChart;
    private BarChart? barChart;
    private bool chartsInitialized;
    private bool IsManager => string.Equals(userInfo.Username, room?.ManagerName, StringComparison.OrdinalIgnoreCase);
    private readonly string[] votes = ["1", "2", "3", "5", "8", "13", "21", "?"];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            StateContainer.OnChange += StateHasChanged;

            userInfo = UserService.GetCurrentUserInfo();
            
            Console.WriteLine($"[Room.OnInitializedAsync] Current user: '{userInfo.Username}', RoomId: {RoomId}");

            // If no username cookie, redirect to join page to capture name
            if (string.IsNullOrWhiteSpace(userInfo.Username))
            {
                Navigation.NavigateTo($"/join/{RoomId}", true);
                return;
            }

            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);
            if (query.TryGetValue("joinError", out var errorValue))
            {
                joinErrorMessage = errorValue.FirstOrDefault();
            }

            var hubUrl = Navigation.ToAbsoluteUri("roomhub");
            Console.WriteLine($"[Room] Connecting to SignalR hub: {hubUrl}");

            hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();

            hubConnection.On("JoinRejected", async (string message) =>
            {
                await InvokeAsync(() =>
                {
                    joinErrorMessage = message;
                    Navigation.NavigateTo($"/join/{RoomId}?joinError={Uri.EscapeDataString(message)}", true);
                });
            });

            hubConnection.On("Update", async () =>
            {
                if (_disposed || hubConnection?.State != HubConnectionState.Connected) return;

                await InvokeAsync(async () =>
                {
                    if (_disposed) return;

                    try
                    {
                        var wasVotingOpen = room?.IsVotingOpen ?? false;
                        await Load();

                        if (wasVotingOpen && room != null && !room.IsVotingOpen && room.Tally?.Count > 0)
                        {
                            showResultsModal = true;
                            chartsInitialized = false;
                            if (resultsModal != null)
                                await resultsModal.ShowAsync();
                        }
                        else if (room != null && room.IsVotingOpen)
                        {
                            showResultsModal = false;
                            chartsInitialized = false;
                            if (resultsModal != null)
                                await resultsModal.HideAsync();
                        }

                        StateHasChanged();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Room.Update] Error during update: {ex.Message}");
                    }
                });
            });

            try
            {
                await hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Room.OnInitializedAsync] Failed to start SignalR connection: {ex.Message}");
            }

            if (!string.IsNullOrWhiteSpace(userInfo.Username) && !_disposed && hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await hubConnection.SendAsync("JoinRoom", RoomId, userInfo);
                    await Load();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Room.OnInitializedAsync] Failed to join room: {ex.Message}");
                    await Load();
                }
            }
            else if (!string.IsNullOrWhiteSpace(userInfo.Username))
            {
                await Load();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Room.OnInitializedAsync] Critical error: {ex.Message}");
        }
    }

    private async Task Load()
    {
        if (_disposed) return;

        try
        {
            Console.WriteLine($"[Room.Load] Starting load for room {RoomId}, user: '{userInfo.Username}'");

            int retries = 3;
            int delayMs = 100;

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    var apiUrl = Navigation.ToAbsoluteUri($"api/rooms/{RoomId}");
                    Console.WriteLine($"[Room.Load] Calling API: {apiUrl}");

                    room = await Http.GetFromJsonAsync<RoomDto>(apiUrl);
                    Console.WriteLine($"[Room.Load] Successfully loaded room {RoomId}");
                    return;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("404"))
                {
                    Console.WriteLine($"[Room.Load] Room {RoomId} not found (404), retry {i + 1}/{retries}");
                    if (i < retries - 1)
                    {
                        await Task.Delay(delayMs);
                        delayMs *= 2;
                        continue;
                    }

                    Console.WriteLine($"[Room.Load] Room {RoomId} not found (404) after {retries} retries");
                    room = null;
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"[Room.Load] HTTP error loading room {RoomId}: {ex.Message}, Status: {ex.StatusCode}");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Room.Load] Unexpected error loading room {RoomId}: {ex.Message}");
                    room = null;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Room.Load] Critical error in Load method: {ex.Message}");
            room = null;
        }
    }

    private async Task StartVoting()
    {
        try
        {
            showResultsModal = false;
            chartsInitialized = false;
            if (resultsModal != null)
                await resultsModal.HideAsync();

            if (_disposed || hubConnection?.State != HubConnectionState.Connected) return;

            try
            {
                await hubConnection.SendAsync("StartVoting", RoomId);
            }
            catch (ObjectDisposedException)
            {
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Room.StartVoting] Error: {ex.Message}");
        }
    }

    private async Task FinishVoting()
    {
        try
        {
            if (_disposed || hubConnection?.State != HubConnectionState.Connected) return;

            try
            {
                await hubConnection.SendAsync("FinishVoting", RoomId);
            }
            catch (ObjectDisposedException)
            {
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Room.FinishVoting] Error: {ex.Message}");
        }
    }

    private async Task CastVote(string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userInfo.Username)) return;
            if (_disposed || hubConnection?.State != HubConnectionState.Connected) return;

            try
            {
                await hubConnection.SendAsync("CastVote", RoomId, userInfo.Username, value);
            }
            catch (ObjectDisposedException)
            {
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Room.CastVote] Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            _disposed = true;
            StateContainer.OnChange -= StateHasChanged;

            if (hubConnection is not null)
            {
                try
                {
                    if (hubConnection.State == HubConnectionState.Connected)
                    {
                        _ = hubConnection.SendAsync("LeaveRoom", RoomId);
                        _ = hubConnection.StopAsync();
                    }
                    _ = hubConnection.DisposeAsync();
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Room.Dispose] Error: {ex.Message}");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (showResultsModal && !chartsInitialized && pieChart != null && barChart != null)
            {
                await RenderChartsIfNeeded();
            }
            else if (showResultsModal && !chartsInitialized && (pieChart == null || barChart == null))
            {
                Console.WriteLine($"[Room.OnAfterRenderAsync] Charts not ready, will retry: pieChart={pieChart != null}, barChart={barChart != null}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Room.OnAfterRenderAsync] Error: {ex.Message}");
        }
    }

    private async Task RenderChartsIfNeeded()
    {
        if (room != null && !room.IsVotingOpen && room.Tally?.Count > 0 && !chartsInitialized)
        {
            try
            {
                await Task.Delay(500);
                await UpdateCharts();
                chartsInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Room.RenderChartsIfNeeded] Error: {ex.Message}");
                chartsInitialized = false;
            }
        }
    }

    private async Task UpdateCharts()
    {
        if (room?.Tally == null || room.Tally.Count == 0) return;
        if (pieChart == null || barChart == null)
        {
            Console.WriteLine($"[Room.UpdateCharts] Charts not ready: pieChart={pieChart != null}, barChart={barChart != null}");
            return;
        }

        try
        {
            var labels = room.Tally.Keys.OrderBy(k => k).ToList();
            var data = labels.Select(k => (double?)room.Tally[k]).ToList();

            var colors = new List<string>
            {
                "rgba(255, 99, 132, 0.8)",
                "rgba(54, 162, 235, 0.8)",
                "rgba(255, 206, 86, 0.8)",
                "rgba(75, 192, 192, 0.8)",
                "rgba(153, 102, 255, 0.8)",
                "rgba(255, 159, 64, 0.8)",
                "rgba(255, 99, 132, 0.8)",
                "rgba(201, 203, 207, 0.8)",
                "rgba(75, 192, 192, 0.8)",
                "rgba(255, 99, 132, 0.8)"
            };

            var pieChartData = new ChartData
            {
                Labels = labels,
                Datasets =
                [
                    new PieChartDataset
                    {
                        Label = "Vote Distribution",
                        Data = data,
                        BackgroundColor = colors.Take(labels.Count).ToList()
                    }
                ]
            };

            var pieOptions = new PieChartOptions
            {
                Responsive = true,
                MaintainAspectRatio = false
            };

            await pieChart.InitializeAsync(pieChartData, pieOptions);

            var barChartData = new ChartData
            {
                Labels = labels,
                Datasets = new List<IChartDataset>
                {
                    new BarChartDataset
                    {
                        Label = "Number of Votes",
                        Data = data,
                        BackgroundColor = new List<string> { "rgba(13, 110, 253, 0.8)" },
                        BorderColor = new List<string> { "rgba(10, 88, 202, 1)" },
                        BorderWidth = new List<double> { 1 }
                    }
                }
            };

            var barOptions = new BarChartOptions
            {
                Responsive = true,
                MaintainAspectRatio = false,
                Scales = new Scales
                {
                    Y = new ChartAxes
                    {
                        BeginAtZero = true,
                        Title = new ChartAxesTitle
                        {
                            Display = true,
                            Text = "Votes"
                        }
                    },
                    X = new ChartAxes
                    {
                        Title = new ChartAxesTitle
                        {
                            Display = true,
                            Text = "Value"
                        }
                    }
                }
            };

            await barChart.InitializeAsync(barChartData, barOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Room.UpdateCharts] Error initializing charts: {ex.Message}");
            chartsInitialized = false;
        }
    }
}
