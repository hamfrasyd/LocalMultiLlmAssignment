# Opsætningsguide – Custom C# Multi-LLM Orchestrator

## 1. Forudsætninger

Installer følgende:

- Git
- .NET 10 SDK
- Ollama
- Docker Desktop

Kontrollér installationerne i PowerShell:

```powershell
git --version
```

```powershell
dotnet --version
```

```powershell
ollama --version
```

```powershell
docker --version
```

Start derefter:

- Docker Desktop
- Ollama

---

## 2. Hent modellerne

Hent modellen til det almindelige Ollama-endpoint:

```powershell
ollama pull qwen3:8b
```

Hent Ollama Docker-image:

```powershell
docker pull ollama/ollama
```

Opret det andet Ollama-endpoint i Docker:

```powershell
docker run -d `
  --name ollama-coder `
  --restart unless-stopped `
  -p 11435:11434 `
  -v ollama-coder:/root/.ollama `
  ollama/ollama
```

Hent coding-modellen i Docker-containeren:

```powershell
docker exec -it ollama-coder ollama pull deepseek-coder:6.7b
```

---

## 3. Kontrollér begge endpoints

Kontrollér endpoint 1:

```powershell
(Invoke-RestMethod -Uri "http://127.0.0.1:11434/api/tags").models.name
```

Det skal blandt andet vise:

```text
qwen3:8b
```

Kontrollér endpoint 2:

```powershell
(Invoke-RestMethod -Uri "http://127.0.0.1:11435/api/tags").models.name
```

Det skal vise:

```text
deepseek-coder:6.7b
```

---

## 4. Klon projektet

Kør:

```powershell
git clone -b crewai-version --single-branch https://github.com/hamfrasyd/LocalMultiLlmAssignment.git
```

Gå ind i projektmappen:

```powershell
cd LocalMultiLlmAssignment
```

---

## 5. Kontrollér konfigurationen

Kør:

```powershell
Get-Content .\workflow-settings.json
```

Konfigurationen skal bruge:

```text
Architect -> qwen3:8b -> http://127.0.0.1:11434
Developer -> deepseek-coder:6.7b -> http://127.0.0.1:11435
Tester -> deepseek-coder:6.7b -> http://127.0.0.1:11435
```

---

## 6. Restore og build

Kør:

```powershell
dotnet restore
```

Derefter:

```powershell
dotnet build
```

Build skal ende med:

```text
Build succeeded
```

---

## 7. Kør workflowet

Kør:

```powershell
dotnet run --project .\src\LocalLlm.Orchestrator -- .\workflow-settings.json
```

Workflowet kører:

```text
Architect -> Developer -> Tester
```

Når programmet spørger:

```text
Apply the Developer and Tester proposals? [y/N]:
```

Skriv:

```text
y
```

Når programmet spørger:

```text
Run dotnet build and dotnet test? [y/N]:
```

Skriv:

```text
y
```

Workflowet skal ende med:

```text
Workflow completed.
```

---

## 8. Kontrollér resultatet

Se Git-ændringer:

```powershell
git diff
```

Se seneste artifact-mappe:

```powershell
Get-ChildItem .\artifacts -Directory |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 1
```

Kontrollér dokumentation:

```powershell
Get-ChildItem .\demo\docs -Recurse
```

Kør tests igen:

```powershell
dotnet test .\demo\DemoApi.Tests\DemoApi.Tests.csproj
```

---

## 9. Deployment-validering

Kør:

```powershell
dotnet publish .\demo\DemoApi\DemoApi.csproj `
  -c Release `
  -o .\publish
```

Kommandoen skal afslutte uden fejl.

---

## 10. Stop og start coding-endpoint

Stop containeren:

```powershell
docker stop ollama-coder
```

Start containeren igen:

```powershell
docker start ollama-coder
```

---

## Kort oversigt

Forløbet er:

```text
Installer programmer
→ Hent qwen3:8b
→ Start Docker Ollama på port 11435
→ Hent deepseek-coder:6.7b
→ Klon projektet
→ Kør dotnet restore
→ Kør dotnet build
→ Kør orchestratoren
→ Godkend filændringer
→ Godkend build og tests
→ Kontrollér git diff
→ Kør dotnet publish
```
