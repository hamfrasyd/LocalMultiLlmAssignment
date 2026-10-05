using System.Text.Json;

namespace LocalLlm.Orchestrator;

public static class SettingsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static WorkflowSettings Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Workflow settings file was not found: {path}");
        }

        var json = File.ReadAllText(path);

        var settings = JsonSerializer.Deserialize<WorkflowSettings>(
            json,
            JsonOptions);

        return settings
            ?? throw new InvalidOperationException(
                "Could not deserialize workflow settings.");
    }
}