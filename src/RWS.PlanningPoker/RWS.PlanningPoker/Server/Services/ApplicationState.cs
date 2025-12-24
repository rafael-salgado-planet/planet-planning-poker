using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace RWS.PlanningPoker.Server.Services;

public class ApplicationState
{
    private readonly ConcurrentDictionary<string, string> _data = new();
    private readonly string _filePath = "appstate.json";
    private readonly IHostApplicationLifetime _lifetime;

    public ApplicationState(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
        LoadFromFile();
        _lifetime.ApplicationStopping.Register(SaveToFile);
    }

    public void PersistAsJson<T>(string key, T value)
    {
        _data[key] = JsonSerializer.Serialize(value);
    }

    public bool TryTakeFromJson<T>(string key, out T? value)
    {
        if (_data.TryGetValue(key, out var json))
        {
            value = JsonSerializer.Deserialize<T>(json);
            return true;
        }
        value = default;
        return false;
    }

    public Task RegisterOnPersisting(Func<Task> callback)
    {
        _lifetime.ApplicationStopping.Register(() => callback());
        return Task.CompletedTask;
    }

    private void SaveToFile()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Ignore errors
        }
    }

    private void LoadFromFile()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(json);
                if (data != null)
                {
                    _data.Clear();
                    foreach (var kv in data)
                    {
                        _data[kv.Key] = kv.Value;
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
    }
}