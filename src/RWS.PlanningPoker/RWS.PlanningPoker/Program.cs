using RWS.PlanningPoker.Components;
using RWS.PlanningPoker.Server.Services;
using RWS.PlanningPoker.Server.Hubs;
using BlazorBootstrap;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs from appsettings.json if specified
var urls = builder.Configuration["Urls"];
if (!string.IsNullOrEmpty(urls))
{
    builder.WebHost.UseUrls(urls);
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
// Removed .AddInteractiveWebAssemblyComponents() to avoid duplicate route mappings

builder.Services.AddControllers(); // Added for API controllers
builder.Services.AddHttpContextAccessor();
builder.Services.AddBlazorBootstrap();
builder.Services.AddSingleton<RoomStore>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<HttpClient>(sp => {
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;
    if (httpContext != null) {
        var scheme = httpContext.Request.Scheme;
        var host = httpContext.Request.Host.Value;
        var pathBase = httpContext.Request.PathBase.Value;
        var baseUri = $"{scheme}://{host}{pathBase}/";
        Console.WriteLine($"[HttpClient] Setting BaseAddress to: {baseUri}");
        return new HttpClient { BaseAddress = new Uri(baseUri) };
    }
    Console.WriteLine("[HttpClient] HttpContext is null, using default HttpClient");
    return new HttpClient();
});
builder.Services.AddSingleton<ApplicationState>(sp => new ApplicationState(sp.GetRequiredService<IHostApplicationLifetime>()));
builder.Services.AddScoped<StateContainer>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseWebAssemblyDebugging(); // removed for server-only
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Remove HTTPS redirect for IIS http-only sub-app hosting
// app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers(); // Added to map API controllers
app.MapHub<RoomHub>("/roomhub");

// Configure base path for IIS virtual directory if present
var basePath = builder.Configuration["BasePath"];
if (!string.IsNullOrWhiteSpace(basePath))
{
    app.UsePathBase(basePath);
}

app.Run();
