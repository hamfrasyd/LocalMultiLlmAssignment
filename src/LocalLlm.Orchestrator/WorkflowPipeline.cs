using System.Text;
using System.Text.Json;

namespace LocalLlm.Orchestrator;

public sealed class WorkflowPipeline
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly WorkflowSettings _settings;
    private readonly OllamaClient _ollamaClient;

    private readonly string _repositoryRoot;
    private readonly string _workspacePath;
    private readonly string _artifactRoot;
    private readonly string _runDirectory;

    public WorkflowPipeline(
        WorkflowSettings settings,
        OllamaClient ollamaClient)
    {
        _settings = settings;
        _ollamaClient = ollamaClient;

        _repositoryRoot = Directory.GetCurrentDirectory();
        _workspacePath = Path.GetFullPath(
            Path.Combine(_repositoryRoot, settings.Workspace));
        _artifactRoot = Path.GetFullPath(
            Path.Combine(_repositoryRoot, settings.ArtifactRoot));
        _runDirectory = Path.Combine(
            _artifactRoot,
            $"run-{DateTime.Now:yyyyMMdd-HHmmss}");

        Directory.CreateDirectory(_runDirectory);
    }

    public async Task RunAsync(
        CancellationToken cancellationToken = default)
    {
        PrintRoutingTable();

        var briefPath = Path.GetFullPath(
            Path.Combine(_repositoryRoot, _settings.BriefFile));
        var brief = await File.ReadAllTextAsync(
            briefPath,
            cancellationToken);
        var repositorySnapshot = RepositorySnapshot.Build(_workspacePath);

        Console.WriteLine("\n[1/3] Running Architect...");
        var architecture = await ChatRoleAsync(
            "Architect",
            Prompts.ArchitectSystem,
            Prompts.Architect(brief, repositorySnapshot),
            cancellationToken: cancellationToken);
        await SaveArtifactAsync(
            "architecture.md",
            architecture,
            cancellationToken);

        Console.WriteLine("\n[2/3] Running Developer...");
        var developerProposal = await GenerateFileProposalAsync(
            "Developer",
            Prompts.DeveloperSystem,
            Prompts.Developer(brief, architecture, repositorySnapshot),
            "developer-proposal",
            testsOnly: false,
            cancellationToken);
        var developerJson = JsonSerializer.Serialize(
            developerProposal,
            JsonOptions);
        await SaveArtifactAsync(
            "developer-proposal.json",
            developerJson,
            cancellationToken);

        Console.WriteLine("\n[3/3] Running Tester...");
        var testerProposal = await GenerateFileProposalAsync(
            "Tester",
            Prompts.TesterSystem,
            Prompts.Tester(
                brief,
                architecture,
                repositorySnapshot,
                developerJson),
            "tester-proposal",
            testsOnly: true,
            cancellationToken);
        var testerJson = JsonSerializer.Serialize(
            testerProposal,
            JsonOptions);
        await SaveArtifactAsync(
            "tester-proposal.json",
            testerJson,
            cancellationToken);

        SafeFileApplier.EnsureNoCollisions(
            developerProposal,
            testerProposal);
        SafeFileApplier.PrintPreview(
            _workspacePath,
            developerProposal,
            testerProposal);

        if (!HumanApproval.Ask("Apply the Developer and Tester proposals?"))
        {
            Console.WriteLine("Stopped before file edits.");
            return;
        }

        SafeFileApplier.Apply(
            _workspacePath,
            developerProposal,
            testerProposal);

        Console.WriteLine("\nBuild and test gate...");
        ProcessResult buildResult;
        ProcessResult testResult;

        if (HumanApproval.Ask("Run dotnet build and dotnet test?"))
        {
            buildResult = await RunBuildAsync(cancellationToken);
            testResult = await RunTestsAsync(cancellationToken);
        }
        else
        {
            buildResult = NotRun("Build was not run because the user did not approve it.");
            testResult = NotRun("Tests were not run because the user did not approve them.");
        }

        await SaveProcessArtifactAsync(
            "build-results.txt",
            buildResult,
            cancellationToken);
        await SaveProcessArtifactAsync(
            "test-results.txt",
            testResult,
            cancellationToken);

        var finalDiff = await CaptureGitDiffAsync(cancellationToken);
        await SaveArtifactAsync(
            "git-diff-final.txt",
            finalDiff,
            cancellationToken);

        Console.WriteLine("\nWorkflow completed.");
        Console.WriteLine($"Artifacts: {_runDirectory}");
        Console.WriteLine();
        Console.WriteLine("Inspect the diff before committing:");
        Console.WriteLine("  git diff");
        Console.WriteLine();
        Console.WriteLine("If satisfied, commit manually.");
    }

    private async Task<FileProposal> GenerateFileProposalAsync(
        string role,
        string systemPrompt,
        string basePrompt,
        string artifactPrefix,
        bool testsOnly,
        CancellationToken cancellationToken)
    {
        var currentPrompt = basePrompt;
        const int maximumAttempts = 3;

        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            Console.WriteLine(
                $"  {role} attempt {attempt}/{maximumAttempts}...");

            var json = await ChatRoleAsync(
                role,
                systemPrompt,
                currentPrompt,
                Schemas.FileProposal,
                cancellationToken);
            await SaveArtifactAsync(
                $"{artifactPrefix}-attempt-{attempt}.json",
                json,
                cancellationToken);

            try
            {
                var proposal = Deserialize<FileProposal>(json);
                SafeFileApplier.EnsureNoDuplicatePaths(proposal);
                SafeFileApplier.EnsureValidAgentPaths(
                    proposal,
                    testsOnly);

                Console.WriteLine($"  {role} proposal accepted.");
                return proposal;
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine($"  {role} proposal rejected:");
                Console.WriteLine($"    {exception.Message}");

                if (attempt == maximumAttempts)
                {
                    throw new InvalidOperationException(
                        $"{role} failed to produce a valid proposal " +
                        $"after {maximumAttempts} attempts.",
                        exception);
                }

                currentPrompt = $"""
                    {basePrompt}

                    IMPORTANT CORRECTION

                    The orchestrator rejected your previous response:
                    {exception.Message}

                    Generate the proposal again from scratch. Correct
                    the validation problem. Return between 1 and 5 unique
                    file paths, complete non-empty file contents, and
                    only paths valid for your assigned role. Do not
                    create project or solution files.

                    Return JSON only.
                    """;
            }
        }

        throw new InvalidOperationException(
            $"{role} unexpectedly exhausted its retry loop.");
    }

    private async Task<string> ChatRoleAsync(
        string role,
        string systemPrompt,
        string userPrompt,
        JsonElement? responseFormat = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = ResolveRole(role);
        Console.WriteLine(
            $"  {role} -> {endpoint.BaseUrl} -> {endpoint.Model}");

        return await _ollamaClient.ChatAsync(
            endpoint,
            systemPrompt,
            userPrompt,
            responseFormat,
            cancellationToken);
    }

    private ModelEndpointSettings ResolveRole(string role)
    {
        if (!_settings.RoleBindings.TryGetValue(role, out var endpointName))
        {
            throw new InvalidOperationException(
                $"No endpoint binding configured for role '{role}'.");
        }

        if (!_settings.Endpoints.TryGetValue(endpointName, out var endpoint))
        {
            throw new InvalidOperationException(
                $"Role '{role}' references unknown endpoint '{endpointName}'.");
        }

        return endpoint;
    }

    private void PrintRoutingTable()
    {
        Console.WriteLine("Configured role routing:");

        foreach (var binding in _settings.RoleBindings)
        {
            var endpoint = ResolveRole(binding.Key);
            Console.WriteLine(
                $"  {binding.Key,-12} -> {binding.Value,-8} " +
                $"-> {endpoint.BaseUrl} -> {endpoint.Model}");
        }
    }

    private async Task<ProcessResult> RunBuildAsync(
        CancellationToken cancellationToken)
    {
        return await ProcessRunner.RunAsync(
            "dotnet",
            ["build", _settings.ApplicationProject],
            _repositoryRoot,
            cancellationToken);
    }

    private async Task<ProcessResult> RunTestsAsync(
        CancellationToken cancellationToken)
    {
        return await ProcessRunner.RunAsync(
            "dotnet",
            ["test", _settings.TestProject, "--no-restore"],
            _repositoryRoot,
            cancellationToken);
    }

    private async Task<string> CaptureGitDiffAsync(
        CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync(
            "git",
            ["diff", "--", _settings.Workspace],
            _repositoryRoot,
            cancellationToken);

        return FormatProcessResult(result);
    }

    private async Task SaveProcessArtifactAsync(
        string name,
        ProcessResult result,
        CancellationToken cancellationToken)
    {
        await SaveArtifactAsync(
            name,
            FormatProcessResult(result),
            cancellationToken);
    }

    private async Task SaveArtifactAsync(
        string name,
        string content,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(_runDirectory, name);
        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    private static ProcessResult NotRun(string reason)
    {
        return new ProcessResult(-1, "", reason);
    }

    private static string FormatProcessResult(ProcessResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Exit code: {result.ExitCode}");
        builder.AppendLine();
        builder.AppendLine("STDOUT");
        builder.AppendLine("======");
        builder.AppendLine(result.StandardOutput);
        builder.AppendLine();
        builder.AppendLine("STDERR");
        builder.AppendLine("======");
        builder.AppendLine(result.StandardError);

        return builder.ToString();
    }

    private static T Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Model returned empty {typeof(T).Name}.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Could not parse model JSON as {typeof(T).Name}.\n\n" +
                $"MODEL OUTPUT:\n{json}",
                exception);
        }
    }
}
