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

        _repositoryRoot =
            Directory.GetCurrentDirectory();

        _workspacePath =
            Path.GetFullPath(
                Path.Combine(
                    _repositoryRoot,
                    settings.Workspace));

        _artifactRoot =
            Path.GetFullPath(
                Path.Combine(
                    _repositoryRoot,
                    settings.ArtifactRoot));

        _runDirectory =
            Path.Combine(
                _artifactRoot,
                $"run-{DateTime.Now:yyyyMMdd-HHmmss}");

        Directory.CreateDirectory(_runDirectory);
    }

    public async Task RunAsync(
        CancellationToken cancellationToken = default)
    {
        PrintRoutingTable();

        var briefPath =
            Path.GetFullPath(
                Path.Combine(
                    _repositoryRoot,
                    _settings.BriefFile));

        var brief =
            await File.ReadAllTextAsync(
                briefPath,
                cancellationToken);

        var initialSnapshot =
            RepositorySnapshot.Build(
                _workspacePath);

        // -------------------------------------------------
        // 1. Architecture
        // -------------------------------------------------

        Console.WriteLine(
            "\n[1/8] Running Architect...");

        var architecture =
            await ChatRoleAsync(
                "Architect",
                Prompts.ArchitectSystem,
                Prompts.Architect(
                    brief,
                    initialSnapshot),
                cancellationToken: cancellationToken);

        await SaveArtifactAsync(
            "architecture.md",
            architecture,
            cancellationToken);

        // -------------------------------------------------
        // 2. Tech lead
        // -------------------------------------------------

        Console.WriteLine(
            "\n[2/8] Running Tech Lead...");

        var ticketJson =
            await ChatRoleAsync(
                "TechLead",
                Prompts.TechLeadSystem,
                Prompts.TechLead(
                    brief,
                    architecture,
                    initialSnapshot),
                Schemas.TicketPlan,
                cancellationToken);

        await SaveArtifactAsync(
            "tickets.json",
            ticketJson,
            cancellationToken);

        var ticketPlan =
            Deserialize<TicketPlan>(ticketJson);

        if (ticketPlan.Tickets.Count != 2)
        {
            throw new InvalidOperationException(
                "Tech Lead must return exactly two tickets.");
        }

        // -------------------------------------------------
        // 3. Two coding workers sequentially
        // -------------------------------------------------
        Console.WriteLine(
            "\n[3/8] Running partitioned Coder A and Coder B...");

        var coderA =
            await GenerateCoderProposalAsync(
                "CoderA",
                ticketPlan.Tickets[0],
                architecture,
                initialSnapshot,
                cancellationToken);

        Console.WriteLine(
            "  Running Coder B...");

        var coderB =
            await GenerateCoderProposalAsync(
                "CoderB",
                ticketPlan.Tickets[1],
                architecture,
                initialSnapshot,
                cancellationToken);

        SafeFileApplier.EnsureNoCollisions(
            coderA,
            coderB);

        SafeFileApplier.PrintPreview(
            _workspacePath,
            coderA,
            coderB);

        if (!HumanApproval.Ask(
                "Apply the two coding proposals?"))
        {
            Console.WriteLine(
                "Stopped before file edits.");

            return;
        }

        SafeFileApplier.Apply(
            _workspacePath,
            coderA,
            coderB);

        var firstDiff =
            await CaptureGitDiffAsync(
                cancellationToken);

        await SaveArtifactAsync(
            "git-diff-after-coders.txt",
            firstDiff,
            cancellationToken);

        // -------------------------------------------------
        // 4. Build and test
        // -------------------------------------------------

        Console.WriteLine(
            "\n[4/8] Build and test gate...");

        if (!HumanApproval.Ask(
                "Run dotnet build and dotnet test?"))
        {
            Console.WriteLine(
                "Stopped before command execution.");

            return;
        }

        var buildResult =
            await RunBuildAsync(
                cancellationToken);

        var testResult =
            await RunTestsAsync(
                cancellationToken);

        await SaveProcessArtifactAsync(
            "build-results.txt",
            buildResult,
            cancellationToken);

        await SaveProcessArtifactAsync(
            "test-results.txt",
            testResult,
            cancellationToken);

        // -------------------------------------------------
        // 5. One controlled repair attempt
        // -------------------------------------------------

        if (buildResult.ExitCode != 0
            || testResult.ExitCode != 0)
        {
            Console.WriteLine(
                "\nBuild or tests failed.");

            if (HumanApproval.Ask(
                    "Ask the Repair worker for one fix attempt?"))
            {
                var failedSnapshot =
                    RepositorySnapshot.Build(
                        _workspacePath);

                var repairJson =
                    await ChatRoleAsync(
                        "Repair",
                        Prompts.RepairSystem,
                        Prompts.Repair(
                            architecture,
                            failedSnapshot,
                            FormatProcessResult(buildResult),
                            FormatProcessResult(testResult)),
                        Schemas.FileProposal,
                        cancellationToken);

                await SaveArtifactAsync(
                    "repair-proposal.json",
                    repairJson,
                    cancellationToken);

                var repair =
                    Deserialize<FileProposal>(
                        repairJson);

                SafeFileApplier.PrintPreview(
                    _workspacePath,
                    repair);

                if (HumanApproval.Ask(
                        "Apply the repair proposal?"))
                {
                    SafeFileApplier.Apply(
                        _workspacePath,
                        repair);

                    if (HumanApproval.Ask(
                            "Re-run build and tests?"))
                    {
                        buildResult =
                            await RunBuildAsync(
                                cancellationToken);

                        testResult =
                            await RunTestsAsync(
                                cancellationToken);

                        await SaveProcessArtifactAsync(
                            "build-results-after-repair.txt",
                            buildResult,
                            cancellationToken);

                        await SaveProcessArtifactAsync(
                            "test-results-after-repair.txt",
                            testResult,
                            cancellationToken);
                    }
                }
            }
        }

        // -------------------------------------------------
        // 6. QA report
        // -------------------------------------------------

        Console.WriteLine(
            "\n[5/8] Running QA...");

        var currentDiff =
            await CaptureGitDiffAsync(
                cancellationToken);

        var qualityReport =
            await ChatRoleAsync(
                "QA",
                Prompts.QaSystem,
                Prompts.Qa(
                    brief,
                    architecture,
                    FormatProcessResult(buildResult),
                    FormatProcessResult(testResult),
                    currentDiff),
                cancellationToken: cancellationToken);

        await SaveArtifactAsync(
            "quality-report.md",
            qualityReport,
            cancellationToken);

        // -------------------------------------------------
        // 7. Documentation
        // -------------------------------------------------

        Console.WriteLine(
            "\n[6/8] Running Documentation worker...");

        var documentationSnapshot =
            RepositorySnapshot.Build(
                _workspacePath);

        var documentationJson =
            await ChatRoleAsync(
                "Docs",
                Prompts.DocsSystem,
                Prompts.Docs(
                    brief,
                    architecture,
                    documentationSnapshot,
                    FormatProcessResult(testResult)),
                Schemas.FileProposal,
                cancellationToken);

        await SaveArtifactAsync(
            "documentation-proposal.json",
            documentationJson,
            cancellationToken);

        var documentationProposal =
            Deserialize<FileProposal>(
                documentationJson);

        SafeFileApplier.PrintPreview(
            _workspacePath,
            documentationProposal);

        if (HumanApproval.Ask(
                "Apply documentation changes?"))
        {
            SafeFileApplier.Apply(
                _workspacePath,
                documentationProposal);
        }

        // -------------------------------------------------
        // 8. Deployment validation
        // -------------------------------------------------

        Console.WriteLine(
            "\n[7/8] Deployment validation...");

        ProcessResult publishResult;

        if (HumanApproval.Ask(
                "Run dotnet publish for deployment validation?"))
        {
            publishResult =
                await RunPublishAsync(
                    cancellationToken);
        }
        else
        {
            publishResult =
                new ProcessResult(
                    -1,
                    "",
                    "Publish was not executed because " +
                    "the user did not approve it.");
        }

        await SaveProcessArtifactAsync(
            "publish-results.txt",
            publishResult,
            cancellationToken);

        var deploymentReport =
            await ChatRoleAsync(
                "Deployment",
                Prompts.DeploymentSystem,
                Prompts.Deployment(
                    architecture,
                    FormatProcessResult(publishResult)),
                cancellationToken: cancellationToken);

        await SaveArtifactAsync(
            "deployment-validation.md",
            deploymentReport,
            cancellationToken);

        var finalDiff =
            await CaptureGitDiffAsync(
                cancellationToken);

        await SaveArtifactAsync(
            "git-diff-final.txt",
            finalDiff,
            cancellationToken);

        // -------------------------------------------------
        // Finished
        // -------------------------------------------------

        Console.WriteLine(
            "\n[8/8] Workflow completed.");

        Console.WriteLine(
            $"Artifacts: {_runDirectory}");

        Console.WriteLine();
        Console.WriteLine(
            "Inspect the diff before committing:");

        Console.WriteLine(
            "  git diff");

        Console.WriteLine();
        Console.WriteLine(
            "If satisfied, commit manually.");
    }

    private async Task<FileProposal>
    GenerateCoderProposalAsync(
        string role,
        WorkTicket ticket,
        string architecture,
        string repositorySnapshot,
        CancellationToken cancellationToken)
    {
        var basePrompt =
            Prompts.Coder(
                role,
                ticket,
                architecture,
                repositorySnapshot);

        var currentPrompt = basePrompt;

        var artifactPrefix =
            role.Equals(
                "CoderA",
                StringComparison.OrdinalIgnoreCase)
                ? "worker-a"
                : "worker-b";

        const int maximumAttempts = 3;

        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            Console.WriteLine(
                $"  {role} attempt {attempt}/{maximumAttempts}...");

            var json =
                await ChatRoleAsync(
                    role,
                    Prompts.CoderSystem,
                    currentPrompt,
                    Schemas.FileProposal,
                    cancellationToken);

            await SaveArtifactAsync(
                $"{artifactPrefix}-proposal-attempt-{attempt}.json",
                json,
                cancellationToken);

            try
            {
                var proposal =
                    Deserialize<FileProposal>(
                        json);

                SafeFileApplier.EnsureNoDuplicatePaths(
                    proposal);

                SafeFileApplier.EnsureValidCoderPaths(
                    proposal);

                await SaveArtifactAsync(
                    $"{artifactPrefix}-proposal.json",
                    json,
                    cancellationToken);

                Console.WriteLine(
                    $"  {role} proposal accepted.");

                return proposal;
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine(
                    $"  {role} proposal rejected:");

                Console.WriteLine(
                    $"    {exception.Message}");

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

                Your previous response was rejected by the
                orchestrator.

                Validation error:

                {exception.Message}

                Generate the proposal again from scratch.

                Correct the validation problem.

                Remember:
                - paths must be unique
                - only DemoApi/ and DemoApi.Tests/ are valid roots
                - do not create .csproj or .sln files
                - return between 1 and 5 files
                - content must contain real complete file contents
                - do not repeat the previous invalid output

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
        var endpoint =
            ResolveRole(role);

        Console.WriteLine(
            $"  {role} -> " +
            $"{endpoint.BaseUrl} -> " +
            $"{endpoint.Model}");

        return await _ollamaClient.ChatAsync(
            endpoint,
            systemPrompt,
            userPrompt,
            responseFormat,
            cancellationToken);
    }

    private ModelEndpointSettings ResolveRole(
        string role)
    {
        if (!_settings.RoleBindings.TryGetValue(
                role,
                out var endpointName))
        {
            throw new InvalidOperationException(
                $"No endpoint binding configured " +
                $"for role '{role}'.");
        }

        if (!_settings.Endpoints.TryGetValue(
                endpointName,
                out var endpoint))
        {
            throw new InvalidOperationException(
                $"Role '{role}' references unknown " +
                $"endpoint '{endpointName}'.");
        }

        return endpoint;
    }

    private void PrintRoutingTable()
    {
        Console.WriteLine(
            "Configured role routing:");

        foreach (var binding in _settings.RoleBindings)
        {
            var endpoint =
                ResolveRole(binding.Key);

            Console.WriteLine(
                $"  {binding.Key,-12} " +
                $"-> {binding.Value,-8} " +
                $"-> {endpoint.BaseUrl} " +
                $"-> {endpoint.Model}");
        }
    }

    private async Task<ProcessResult> RunBuildAsync(
        CancellationToken cancellationToken)
    {
        return await ProcessRunner.RunAsync(
            "dotnet",
            [
                "build",
                _settings.ApplicationProject
            ],
            _repositoryRoot,
            cancellationToken);
    }

    private async Task<ProcessResult> RunTestsAsync(
        CancellationToken cancellationToken)
    {
        return await ProcessRunner.RunAsync(
            "dotnet",
            [
                "test",
                _settings.TestProject,
                "--no-restore"
            ],
            _repositoryRoot,
            cancellationToken);
    }

    private async Task<ProcessResult> RunPublishAsync(
        CancellationToken cancellationToken)
    {
        var publishDirectory =
            Path.Combine(
                _runDirectory,
                "publish");

        return await ProcessRunner.RunAsync(
            "dotnet",
            [
                "publish",
                _settings.ApplicationProject,
                "-c",
                "Release",
                "-o",
                publishDirectory
            ],
            _repositoryRoot,
            cancellationToken);
    }

    private async Task<string> CaptureGitDiffAsync(
        CancellationToken cancellationToken)
    {
        var result =
            await ProcessRunner.RunAsync(
                "git",
                [
                    "diff",
                    "--",
                    _settings.Workspace
                ],
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
        var path =
            Path.Combine(
                _runDirectory,
                name);

        await File.WriteAllTextAsync(
            path,
            content,
            cancellationToken);
    }

    private static string FormatProcessResult(
        ProcessResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine(
            $"Exit code: {result.ExitCode}");

        builder.AppendLine();
        builder.AppendLine("STDOUT");
        builder.AppendLine("======");
        builder.AppendLine(
            result.StandardOutput);

        builder.AppendLine();
        builder.AppendLine("STDERR");
        builder.AppendLine("======");
        builder.AppendLine(
            result.StandardError);

        return builder.ToString();
    }

    private static T Deserialize<T>(
        string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(
                    json,
                    JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Model returned empty {typeof(T).Name}.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Could not parse model JSON as " +
                $"{typeof(T).Name}.\n\n" +
                $"MODEL OUTPUT:\n{json}",
                exception);
        }
    }
}