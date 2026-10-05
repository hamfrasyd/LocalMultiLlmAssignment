namespace LocalLlm.Orchestrator;

public static class SafeFileApplier
{
    public static void PrintPreview(
        string workspacePath,
        params FileProposal[] proposals)
    {
        Console.WriteLine();
        Console.WriteLine("Proposed file edits:");
        Console.WriteLine("--------------------");

        foreach (var proposal in proposals)
        {
            Console.WriteLine(proposal.Summary);

            foreach (var file in proposal.Files)
            {
                var fullPath = GetSafeFullPath(
                    workspacePath,
                    file.Path);

                var operation = File.Exists(fullPath)
                    ? "REPLACE"
                    : "CREATE";

                Console.WriteLine(
                    $"  [{operation}] {file.Path} " +
                    $"({file.Content.Length} characters)");
            }
        }

        Console.WriteLine();
    }

    public static void EnsureNoCollisions(
        params FileProposal[] proposals)
    {
        var duplicatePaths = proposals
            .SelectMany(proposal => proposal.Files)
            .GroupBy(
                file => Normalize(file.Path),
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicatePaths.Count > 0)
        {
            throw new InvalidOperationException(
                "Multiple workers proposed edits to the same file:\n" +
                string.Join(
                    Environment.NewLine,
                    duplicatePaths));
        }
    }

    public static void EnsureValidAgentPaths(
        FileProposal proposal,
        bool testsOnly)
    {
        if (proposal.Files.Count is < 1 or > 5)
        {
            throw new InvalidOperationException(
                $"An agent proposed {proposal.Files.Count} files. " +
                "The allowed range is 1 to 5.");
        }

        foreach (var file in proposal.Files)
        {
            var normalizedPath = Normalize(file.Path);

            var isTestPath =
                normalizedPath.StartsWith(
                    "DemoApi.Tests/",
                    StringComparison.OrdinalIgnoreCase);
            var isAllowed =
                isTestPath
                || (!testsOnly
                    && (normalizedPath.StartsWith(
                            "DemoApi/",
                            StringComparison.OrdinalIgnoreCase)
                        || normalizedPath.StartsWith(
                            "docs/",
                            StringComparison.OrdinalIgnoreCase)));

            if (!isAllowed)
            {
                throw new InvalidOperationException(
                    $"{(testsOnly ? "Tester" : "Developer")} proposed " +
                    $"an invalid path: " +
                    $"{file.Path}. " +
                    (testsOnly
                        ? "Tester paths must begin with DemoApi.Tests/."
                        : "Developer paths must begin with DemoApi/, " +
                          "DemoApi.Tests/, or docs/."));
            }

            if (normalizedPath.EndsWith(
                    ".csproj",
                    StringComparison.OrdinalIgnoreCase)
                || normalizedPath.EndsWith(
                    ".sln",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"An agent attempted to create or replace " +
                    $"a project/solution file: {file.Path}");
            }

            if (string.IsNullOrWhiteSpace(file.Content))
            {
                throw new InvalidOperationException(
                    $"An agent returned empty content for: " +
                    file.Path);
            }
        }
    }

    private static string NormalizeModelPath(string path)
    {
        var normalized = path
            .Trim()
            .Replace('\\', '/')
            .TrimStart('/');

        // Handle a common LLM mistake:
        //
        // DemoApi.Controllers.TodosController.cs
        //
        // should be:
        //
        // DemoApi/Controllers/TodosController.cs

        if (!normalized.Contains('/'))
        {
            const string testsPrefix = "DemoApi.Tests.";
            const string appPrefix = "DemoApi.";

            if (normalized.StartsWith(
                    testsPrefix,
                    StringComparison.OrdinalIgnoreCase)
                && normalized.EndsWith(
                    ".cs",
                    StringComparison.OrdinalIgnoreCase))
            {
                var remainder =
                    normalized[
                        testsPrefix.Length..^3];

                normalized =
                    "DemoApi.Tests/"
                    + remainder.Replace('.', '/')
                    + ".cs";
            }
            else if (normalized.StartsWith(appPrefix, StringComparison.OrdinalIgnoreCase) && normalized.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var remainder =
                    normalized[
                        appPrefix.Length..^3];

                normalized =
                    "DemoApi/"
                    + remainder.Replace('.', '/')
                    + ".cs";
            }
        }

        return normalized;
    }

    public static void EnsureNoDuplicatePaths(
        FileProposal proposal)
    {
        var duplicatePaths = proposal.Files
            .GroupBy(
                file => Normalize(file.Path),
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicatePaths.Count > 0)
        {
            throw new InvalidOperationException(
                "A coding worker proposed the same file more than once:\n" +
                string.Join(
                    Environment.NewLine,
                    duplicatePaths));
        }
    }
    public static void Apply(
        string workspacePath,
        params FileProposal[] proposals)
    {
        foreach (var proposal in proposals)
        {
            foreach (var file in proposal.Files)
            {
                var fullPath = GetSafeFullPath(
                    workspacePath,
                    file.Path);

                var directory = Path.GetDirectoryName(fullPath);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(
                    fullPath,
                    file.Content);
            }
        }
    }

    private static string GetSafeFullPath(
        string workspacePath,
        string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidOperationException(
                "Model proposed an empty file path.");
        }
        
        relativePath =
                NormalizeModelPath(relativePath);

        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException(
                $"Absolute paths are not allowed: {relativePath}");
        }

        var workspaceFullPath =
            Path.GetFullPath(workspacePath);

        var candidate =
            Path.GetFullPath(
                Path.Combine(
                    workspaceFullPath,
                    relativePath));

        var requiredPrefix =
            workspaceFullPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(
                requiredPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Model attempted to write outside " +
                $"the workspace: {relativePath}");
        }

        return candidate;
    }

    private static string Normalize(string path)
    {
        return NormalizeModelPath(path);
    }
}