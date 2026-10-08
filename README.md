# Local Multi-LLM Assignment

This repository contains two ways to run an Architect → Developer → Tester
workflow with local Ollama models:

| Choose this | When you want | Start here |
| --- | --- | --- |
| **Custom C# orchestrator** | The workflow integrated with this repository's .NET solution, demo API, approval prompts, and build/test steps | [`src/LocalLlm.Orchestrator/`](./src/LocalLlm.Orchestrator/), [`workflow-settings.json`](./workflow-settings.json), and [`C-sharp-orchestrator-guide.md`](./C-sharp-orchestrator-guide.md) |
| **CrewAI** | The Python CrewAI implementation using the same two local model endpoints | [`CrewAI_Setup_FIle/crew_demo.py`](./CrewAI_Setup_FIle/crew_demo.py) and [`CrewAi-guide.md`](./CrewAi-guide.md) |

Both workflows use `qwen3:8b` for the Architect and `deepseek-coder:6.7b`
for the Developer and Tester. They expect Ollama endpoints at
`127.0.0.1:11434` and `127.0.0.1:11435`.

## Custom C# orchestrator

This is the integrated .NET workflow. It reads the feature request from
[`input/feature-brief.md`](./input/feature-brief.md), works against the demo
application, and saves run details under [`artifacts/`](./artifacts).

### Main files

- [`src/LocalLlm.Orchestrator/Program.cs`](./src/LocalLlm.Orchestrator/Program.cs):
  application entry point.
- [`src/LocalLlm.Orchestrator/WorkflowPipeline.cs`](./src/LocalLlm.Orchestrator/WorkflowPipeline.cs):
  orchestrates the Architect, Developer, and Tester stages.
- [`src/LocalLlm.Orchestrator/OllamaClient.cs`](./src/LocalLlm.Orchestrator/OllamaClient.cs):
  communicates with the local Ollama endpoints.
- [`src/LocalLlm.Orchestrator/SafeFileApplier.cs`](./src/LocalLlm.Orchestrator/SafeFileApplier.cs)
  and [`src/LocalLlm.Orchestrator/HumanApproval.cs`](./src/LocalLlm.Orchestrator/HumanApproval.cs):
  apply proposed changes with confirmation.
- [`workflow-settings.json`](./workflow-settings.json): model endpoints,
  role bindings, workspace, projects, brief, and artifact location.
- [`demo/DemoApi/`](./demo/DemoApi/) and
  [`demo/DemoApi.Tests/`](./demo/DemoApi.Tests/): application and tests the
  workflow targets.

### Run

Prerequisites: .NET 10 SDK, Git, Ollama, and Docker Desktop. Start Ollama and
Docker Desktop, then follow the setup steps in
[`C-sharp-orchestrator-guide.md`](./C-sharp-orchestrator-guide.md) to pull the
models and start the coding endpoint on port `11435`.

From the repository root, restore and build, then start the workflow:

```powershell
dotnet restore
dotnet build
dotnet run --project .\src\LocalLlm.Orchestrator -- .\workflow-settings.json
```

The workflow asks before applying agent proposals and before running the demo
build and tests. Review the changes with `git diff`. To run the tests
independently:

```powershell
dotnet test .\demo\DemoApi.Tests\DemoApi.Tests.csproj
```

## CrewAI

The CrewAI entry point included in this repository is
[`CrewAI_Setup_FIle/crew_demo.py`](./CrewAI_Setup_FIle/crew_demo.py). Its full
setup and two-endpoint instructions are in
[`CrewAi-guide.md`](./CrewAi-guide.md).

That guide also describes a separate `CrewAIDemo` checkout and files such as
`requirements.txt`, `check_endpoints.py`, and `test_todo.py`; those files are
not present at this repository's root. For the script included here, create
and activate a Python 3.11 environment, install CrewAI, then run it from the
repository root:

```powershell
py -3.11 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install crewai
python .\CrewAI_Setup_FIle\crew_demo.py
```

Before running it, follow [`CrewAi-guide.md`](./CrewAi-guide.md) to install the
models and start the second Ollama endpoint on port `11435`. The included
script writes generated `todo.py` and `test_todo.py` files into
`CrewAI_Setup_FIle`. It also attempts to stage and commit each generated file,
so inspect the script and your Git working tree before running it.

## Guides and other project files

- [`C-sharp-orchestrator-guide.md`](./C-sharp-orchestrator-guide.md): detailed
  C# prerequisites, endpoint setup, execution, and validation.
- [`CrewAi-guide.md`](./CrewAi-guide.md): detailed CrewAI prerequisites,
  endpoint setup, and the standalone CrewAI project workflow.
- [`demo/docs/`](./demo/docs/): API documentation, design notes, and runbook
  for the sample API.
