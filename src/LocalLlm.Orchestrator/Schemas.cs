using System.Text.Json;

namespace LocalLlm.Orchestrator;

public static class Schemas
{
    public static JsonElement TicketPlan { get; } =
        Parse("""
        {
          "type": "object",
          "properties": {
            "tickets": {
              "type": "array",
              "minItems": 2,
              "maxItems": 2,
              "items": {
                "type": "object",
                "properties": {
                  "id": {
                    "type": "string"
                  },
                  "title": {
                    "type": "string"
                  },
                  "goal": {
                    "type": "string"
                  },
                  "acceptanceCriteria": {
                    "type": "array",
                    "items": {
                      "type": "string"
                    }
                  },
                  "ownedPaths": {
                    "type": "array",
                    "items": {
                      "type": "string"
                    }
                  },
                  "dependsOn": {
                    "type": "array",
                    "items": {
                      "type": "string"
                    }
                  }
                },
                "required": [
                  "id",
                  "title",
                  "goal",
                  "acceptanceCriteria",
                  "ownedPaths",
                  "dependsOn"
                ]
              }
            }
          },
          "required": [
            "tickets"
          ]
        }
        """);

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