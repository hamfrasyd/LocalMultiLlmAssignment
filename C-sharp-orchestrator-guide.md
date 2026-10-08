# Opsætningsguide - Custom C# Orchestrator

## 1. Forudsætninger

Installer følgende:

- Git
- .NET 10 SDK
- Ollama
- Docker Desktop

Hvis du ikke har disse, så kør følgende i **PowerShell som Administrator**.

### Git - install/update

```powershell
winget install --id Git.Git --exact --source winget --silent --accept-package-agreements --accept-source-agreements
```

### .NET 10 SDK - install/update

```powershell
winget install --id Microsoft.DotNet.SDK.10 --exact --source winget --silent --accept-package-agreements --accept-source-agreements
```

### Ollama - install/update

```powershell
winget install --id Ollama.Ollama --exact --source winget --silent --accept-package-agreements --accept-source-agreements
```

### Docker Desktop - install/update

```powershell
winget install --id Docker.DockerDesktop --exact --source winget --silent --accept-package-agreements --accept-source-agreements
```

Start **Docker Desktop** efter installation.

Kontrollér installationerne:

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

---

## 2. Hent Qwen-modellen

Kontrollér om Ollama allerede kører:

```powershell
curl.exe http://localhost:11434
```

Hvis Ollama ikke allerede kører, start den:

```powershell
ollama serve
```

Hent modellen til **Architect - Endpoint 1, port 11434**.

Hvis modellen allerede er installeret, kan dette trin springes over.

```powershell
ollama pull qwen3:8b
```

Kontrollér at modellen er installeret:

```powershell
ollama list
```

Du skal kunne se:

```text
qwen3:8b
```

---

## 3. Start det andet Ollama-endpoint i Docker

Sørg for, at **Docker Desktop** kører.

Hent Ollama Docker-image:

```powershell
docker pull ollama/ollama
```

Opret **Endpoint 2 på port 11435**.

### a) Hvis du ikke bruger en kompatibel NVIDIA GPU

```powershell
docker run -d --name ollama-coder --restart unless-stopped -p 127.0.0.1:11435:11434 -v ollama-coder:/root/.ollama ollama/ollama
```

### b) Hvis du har en kompatibel NVIDIA GPU

```powershell
docker run -d --name ollama-coder --restart unless-stopped --gpus=all -p 127.0.0.1:11435:11434 -v ollama-coder:/root/.ollama ollama/ollama
```

### c) Hvis containeren allerede findes

Start den eksisterende container:

```powershell
docker start ollama-coder
```

---

## 4. Hent DeepSeek-modellen

Hent modellen til **Developer og Tester** inde i Docker-containeren:

```powershell
docker exec -it ollama-coder ollama pull deepseek-coder:6.7b
```

Kontrollér at modellen findes på Endpoint 2:

```powershell
docker exec -it ollama-coder ollama list
```

Du skal kunne se:

```text
deepseek-coder:6.7b
```

### Kontrollér begge endpoints

Kontrollér **Endpoint 1 - port 11434**:

```powershell
(Invoke-RestMethod -Uri "http://127.0.0.1:11434/api/tags").models.name
```

Det skal vise:

```text
qwen3:8b
```

Kontrollér **Endpoint 2 - port 11435**:

```powershell
(Invoke-RestMethod -Uri "http://127.0.0.1:11435/api/tags").models.name
```

Det skal vise:

```text
deepseek-coder:6.7b
```

---

## 5. Klon projektet

Åbn et nyt PowerShell-vindue.

Gå til `source\repos`, hvis du ikke allerede er der:

```powershell
if ((Get-Location).Path -ne "$HOME\source\repos") { Set-Location "$HOME\source\repos" }
```

Klon projektet:

```powershell
git clone https://github.com/hamfrasyd/LocalMultiLlmAssignment.git
```

Gå ind i projektmappen:

```powershell
cd LocalMultiLlmAssignment
```

---

## 6. Kontrollér konfigurationen

Kør:

```powershell
Get-Content .\workflow-settings.json
```

Kontrollér at konfigurationen matcher:

```json
"general": {
  "baseUrl": "http://127.0.0.1:11434",
  "model": "qwen3:8b"
}
```

```json
"coding": {
  "baseUrl": "http://127.0.0.1:11435",
  "model": "deepseek-coder:6.7b"
}
```

```json
"roleBindings": {
  "Architect": "general",
  "Developer": "coding",
  "Tester": "coding"
}
```

---

## 7. Restore og build

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

## 8. Kør workflowet

Kør:

```powershell
dotnet run --project .\src\LocalLlm.Orchestrator -- .\workflow-settings.json
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

## 9. Kontrollér resultatet

Se Git-ændringer:

```powershell
git diff
```

Se seneste artifact-mappe:

```powershell
Get-ChildItem .\artifacts -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
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

## 10. Deployment-validering

Kør:

```powershell
dotnet publish .\demo\DemoApi\DemoApi.csproj -c Release -o .\publish
```

Kommandoen skal afslutte uden fejl.
