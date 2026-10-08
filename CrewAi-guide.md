# Opsætningsguide - CrewAI

## 1. Forudsætninger

Installer følgende:

- Python 3.11
- Ollama
- Git
- Docker Desktop

Hvis du ikke har disse, så kør følgende i **PowerShell som Administrator**.

### Python 3.11 - install/update

```powershell
winget install --id Python.Python.3.11 --exact --source winget --silent --accept-package-agreements --accept-source-agreements
```

### Ollama - install/update

```powershell
winget install --id Ollama.Ollama --exact --source winget --silent --accept-package-agreements --accept-source-agreements
```

### Git - install/update

```powershell
winget install --id Git.Git --exact --source winget --silent --accept-package-agreements --accept-source-agreements
```

### Docker Desktop - install/update

```powershell
winget install --id Docker.DockerDesktop --exact --source winget --silent --accept-package-agreements --accept-source-agreements
```

Start **Docker Desktop** efter installation.

Kontrollér installationerne:

```powershell
py -3.11 --version
```

```powershell
ollama --version
```

```powershell
git --version
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

---

## 5. Klon projektet

Åbn et nyt PowerShell-vindue.

Gå til `source\repos`, hvis du ikke allerede er der:

```powershell
if ((Get-Location).Path -ne "$HOME\source\repos") { Set-Location "$HOME\source\repos" }
```

Klon projektet:

```powershell
git clone https://github.com/JaisAndersen/CrewAIDemo.git
```

Gå ind i projektmappen:

```powershell
cd CrewAIDemo
```

---

## 6. Opret Python-miljø

Opret et virtuelt Python-miljø:

```powershell
py -3.11 -m venv venv
```

Aktivér miljøet:

```powershell
.\venv\Scripts\Activate.ps1
```

Hvis PowerShell blokerer aktiveringen, kør først:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
```

Aktivér derefter miljøet igen:

```powershell
.\venv\Scripts\Activate.ps1
```

Når miljøet er aktivt, vil terminalen typisk starte med:

```text
(venv)
```

---

## 7. Installer dependencies

Opdatér pip:

```powershell
python -m pip install --upgrade pip
```

Installer projektets dependencies:

```powershell
pip install -r requirements.txt
```

Vent til installationen er færdig.

---

## 8. Kontrollér begge endpoints

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

Kør derefter projektets endpoint-validering:

```powershell
python check_endpoints.py
```

Begge endpoints skal være markeret som aktive.

Til sidst skal der stå:

```text
Alt OK - begge endpoints er oppe og ser ud til kun at vaere tilgaengelige lokalt.
```

---

## 9. Kør CrewAI-workflowet

Kør:

```powershell
python crew_demo.py
```

Workflowet kører:

```text
Architect → Developer → Tester
```

Architect bruger:

```text
qwen3:8b
```

Developer og Tester bruger:

```text
deepseek-coder:6.7b
```

Når workflowet er færdigt, skal projektmappen blandt andet indeholde:

```text
todo.py
test_todo.py
```

---

## 10. Kør tests

Kør:

```powershell
pytest -v test_todo.py
```

Tests skal afslutte uden fejl.
