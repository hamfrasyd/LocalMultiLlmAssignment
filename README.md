# Local Multi-LLM Assignment

This repository contains two local multi-LLM workflows:

- **Custom C# Orchestrator**
- **CrewAI**

Both use:

| Role | Model | Endpoint |
| --- | --- | --- |
| Architect | `qwen3:8b` | `127.0.0.1:11434` |
| Developer | `deepseek-coder:6.7b` | `127.0.0.1:11435` |
| Tester | `deepseek-coder:6.7b` | `127.0.0.1:11435` |

# Custom C# Orchestrator

Full setup guide:

[`C-sharp-orchestrator-guide.md`](./C-sharp-orchestrator-guide.md)

Run from the repository root:

```powershell
dotnet restore
dotnet build
dotnet run --project .\src\LocalLlm.Orchestrator -- .\workflow-settings.json
```

Main files:

- [`workflow-settings.json`](./workflow-settings.json)
- [`input/feature-brief.md`](./input/feature-brief.md)
- [`src/LocalLlm.Orchestrator/`](./src/LocalLlm.Orchestrator/)
- [`demo/`](./demo/)
- [`artifacts/`](./artifacts/)

# CrewAI

Full setup guide:

[`CrewAi-guide.md`](./CrewAi-guide.md)

The standalone CrewAI demo is cloned from:

`https://github.com/JaisAndersen/CrewAIDemo`

Main commands after setup:

```powershell
python check_endpoints.py
python crew_demo.py
pytest -v test_todo.py
```

A copy of the CrewAI implementation is also included here:

[`CrewAI_Setup_FIle/crew_demo.py`](./CrewAI_Setup_FIle/crew_demo.py)

# Endpoint check

```powershell
(Invoke-RestMethod -Uri "http://127.0.0.1:11434/api/tags").models.name
(Invoke-RestMethod -Uri "http://127.0.0.1:11435/api/tags").models.name
```

Expected:

```text
11434 -> qwen3:8b
11435 -> deepseek-coder:6.7b
```
