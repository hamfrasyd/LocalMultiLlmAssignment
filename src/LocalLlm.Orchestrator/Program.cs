using LocalLlm.Orchestrator;

var settingsPath =
    args.Length > 0
        ? args[0]
        : "workflow-settings.json";

try
{
    var settings =
        SettingsLoader.Load(
            settingsPath);

    using var ollamaClient =
        new OllamaClient();

    var pipeline =
        new WorkflowPipeline(
            settings,
            ollamaClient);

    await pipeline.RunAsync();
}
catch (Exception exception)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "WORKFLOW FAILED");
    Console.Error.WriteLine(
        "===============");
    Console.Error.WriteLine(
        exception);

    Environment.ExitCode = 1;
}