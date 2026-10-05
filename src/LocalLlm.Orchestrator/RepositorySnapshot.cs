using System.Text;

namespace LocalLlm.Orchestrator;

public static class RepositorySnapshot
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".csproj",
            ".sln",
            ".json",
            ".md"
        };

    public static string Build(
        string workspacePath,
        int maxTotalCharacters = 12_000)
    {
        var workspaceFullPath = Path.GetFullPath(workspacePath);

        if (!Directory.Exists(workspaceFullPath))
        {
            throw new DirectoryNotFoundException(
                $"Workspace not found: {workspaceFullPath}");
        }

        var builder = new StringBuilder();

        var files = Directory
            .EnumerateFiles(
                workspaceFullPath,
                "*",
                SearchOption.AllDirectories)
            .Where(path => !IsIgnored(path))
            .Where(IsUsefulFile)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(
                workspaceFullPath,
                file);

            string contents;

            try
            {
                contents = File.ReadAllText(file);
            }
            catch
            {
                continue;
            }

            var section = $"""
                ===== FILE: {relativePath} =====
                {contents}

                """;

            if (builder.Length + section.Length > maxTotalCharacters)
            {
                builder.AppendLine(
                    "===== SNAPSHOT TRUNCATED =====");

                break;
            }

            builder.AppendLine(section);
        }

        return builder.ToString();
    }

    private static bool IsUsefulFile(string path)
    {
        var fileName = Path.GetFileName(path);

        if (fileName.Equals(
                "Dockerfile",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return AllowedExtensions.Contains(
            Path.GetExtension(path));
    }

    private static bool IsIgnored(string path)
    {
        var normalized = path.Replace('\\', '/');

        return normalized.Contains("/bin/")
            || normalized.Contains("/obj/")
            || normalized.Contains("/.git/")
            || normalized.Contains("/artifacts/")
            || normalized.Contains("/.vs/")
            || normalized.Contains("/node_modules/")
            || normalized.EndsWith("/.env",
                StringComparison.OrdinalIgnoreCase);
    }
}