using System.Text.Json;

namespace LocalLlm.Orchestrator;

public static class Schemas
{
    public static JsonElement FileProposal { get; } =
        Parse("""
        {
          "type": "object",
          "properties": {
            "summary": {
              "type": "string"
            },
            "files": {
              "type": "array",
              "minItems": 1,
              "maxItems": 5,
              "items": {
                "type": "object",
                "properties": {
                  "path": {
                    "type": "string"
                  },
                  "content": {
                    "type": "string"
                  }
                },
                "required": [
                  "path",
                  "content"
                ]
              }
            }
          },
          "required": [
            "summary",
            "files"
          ]
        }
        """);

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
