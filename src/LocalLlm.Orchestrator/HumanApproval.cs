namespace LocalLlm.Orchestrator;

public static class HumanApproval
{
    public static bool Ask(string question)
    {
        Console.Write($"{question} [y/N]: ");

        var answer = Console.ReadLine();

        return string.Equals(
            answer?.Trim(),
            "y",
            StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                answer?.Trim(),
                "yes",
                StringComparison.OrdinalIgnoreCase);
    }
}