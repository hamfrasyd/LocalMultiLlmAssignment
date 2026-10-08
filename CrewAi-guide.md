# Opsætningsguide – CrewAI

## 1. Forudsætninger

Installer følgende:

- Python 3.11
- Ollama
- Git

Kontrollér installationerne i PowerShell:

```powershell
py -3.11 --version
```

```powershell
ollama --version
```

```powershell
git --version
```

---

## 2. Hent Qwen-modellen

Sørg for, at Ollama-programmet er startet.

Hent modellen til Architect:

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

## 3. Start det andet Ollama-endpoint

Åbn et **nyt PowerShell-vindue**.

Kør:

```powershell
$env:OLLAMA_HOST = "127.0.0.1:11435"
```

Derefter:

```powershell
ollama serve
```

Lad dette PowerShell-vindue stå åbent, mens demoen køres.

Det første Ollama-endpoint kører på:

```text
127.0.0.1:11434
```

Det andet Ollama-endpoint kører på:

```text
127.0.0.1:11435
```

---

## 4. Hent DeepSeek-modellen

Åbn endnu et **nyt PowerShell-vindue**.

Peg Ollama CLI mod endpoint 2:

```powershell
$env:OLLAMA_HOST = "127.0.0.1:11435"
```

Hent modellen til Developer og Tester:

```powershell
ollama pull deepseek-coder:6.7b
```

Kontrollér at modellen findes:

```powershell
ollama list
```

Du skal kunne se:

```text
deepseek-coder:6.7b
```

PowerShell-vinduet med `ollama serve` fra trin 3 skal stadig være åbent.

---

## 5. Klon projektet

Åbn et nyt PowerShell-vindue.

Kør:

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

Aktivér derefter igen:

```powershell
.\venv\Scripts\Activate.ps1
```

Når miljøet er aktivt, vil terminalen typisk starte med:

```text
(venv)
```

---

## 7. Installer dependencies

Kør:

```powershell
python -m pip install --upgrade pip
```

Derefter:

```powershell
pip install -r requirements.txt
```

Vent til installationen er færdig.

---

## 8. Kontrollér begge endpoints

Kør:

```powershell
python check_endpoints.py
```

Begge endpoints skal være markeret som aktive.

Til sidst skal der stå:

```text
Alt OK - begge endpoints er oppe og ser ud til kun at vaere tilgaengelige lokalt.
```

Hvis et endpoint fejler, kontrollér at:

```text
127.0.0.1:11434 kører Ollama
127.0.0.1:11435 kører det ekstra Ollama-endpoint
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

---

## Kort oversigt

Forløbet er:

```text
Installer Python 3.11, Ollama og Git
→ Hent qwen3:8b
→ Start Ollama endpoint 2 på port 11435
→ Hent deepseek-coder:6.7b til endpoint 2
→ Klon CrewAIDemo
→ Opret Python virtual environment
→ Installer requirements.txt
→ Kontrollér begge endpoints
→ Kør crew_demo.py
→ Kør pytest på test_todo.py
```
