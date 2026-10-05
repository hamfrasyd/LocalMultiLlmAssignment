namespace LocalLlm.Orchestrator;

public sealed class WorkflowSettings
{
    public Dictionary<string, ModelEndpointSettings> Endpoints { get; init; } = [];
    public Dictionary<string, string> RoleBindings { get; init; } = [];

    public string Workspace { get; init; } = "demo";
    public string ApplicationProject { get; init; } = "";
    public string TestProject { get; init; } = "";
    public string BriefFile { get; init; } = "";
    public string ArtifactRoot { get; init; } = "artifacts";
}

public sealed class ModelEndpointSettings
{
    public string BaseUrl { get; init; } = "";
    public string Model { get; init; } = "";
}

public sealed class TicketPlan
{
    public List<WorkTicket> Tickets { get; init; } = [];
}

public sealed class WorkTicket
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Goal { get; init; } = "";
    public List<string> AcceptanceCriteria { get; init; } = [];
    public List<string> OwnedPaths { get; init; } = [];
    public List<string> DependsOn { get; init; } = [];
}

public sealed class FileProposal
{
    public string Summary { get; init; } = "";
    public List<ProposedFile> Files { get; init; } = [];
}

public sealed class ProposedFile
{
    public string Path { get; init; } = "";
    public string Content { get; init; } = "";
}

public sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);