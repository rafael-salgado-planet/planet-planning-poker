using System.Text;
using System.Text.Json;

namespace RWS.PlanningPoker.Server.Services;

public record UserCookieRecord(string? Username, string? Id);

public interface IUserService
{    
    UserCookieRecord GetCurrentUserInfo();
    void SetCurrentUser(string username);
    void ClearCurrentUser();
}

public class UserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CookieName = "planningpoker_UserIdentity";
 
    private record UserCookie(string Username, string Id);

    public UserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public UserCookieRecord GetCurrentUserInfo()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return new UserCookieRecord(null, null);

        var raw = httpContext.Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(raw)) return new UserCookieRecord(null, null);

        var decoded = TryDecode(raw);
        if (!string.IsNullOrWhiteSpace(decoded))
        {
            try
            {
                var obj = JsonSerializer.Deserialize<UserCookie>(decoded);
                return new UserCookieRecord(obj?.Username, obj?.Id);
            }
            catch { }
        }

        return new UserCookieRecord(null, null);
    }

    public void SetCurrentUser(string username)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null || string.IsNullOrWhiteSpace(username)) return;

        var trimmedUsername = username.Trim();
        var payload = new UserCookie(trimmedUsername, Guid.NewGuid().ToString());

        // If a cookie exists, reuse its id when possible
        var raw = httpContext.Request.Cookies[CookieName];
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var decoded = TryDecode(raw);
            if (!string.IsNullOrWhiteSpace(decoded))
            {
                try
                {
                    var existing = JsonSerializer.Deserialize<UserCookie>(decoded);
                    if (!string.IsNullOrWhiteSpace(existing?.Id))
                    {
                        payload = payload with { Id = existing.Id };
                    }
                }
                catch { }
            }
        }

        var json = JsonSerializer.Serialize(payload);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        try
        {
            httpContext.Response.Cookies.Append(CookieName, encoded,
                new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(30),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    Path = "/"
                });
        }
        catch
        {
            // ignore
        }
    }

    public void ClearCurrentUser()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        try
        {
            httpContext.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
        }
        catch { }
    }

    private static string? TryDecode(string raw)
    {
        try
        {
            var bytes = Convert.FromBase64String(raw);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }
}