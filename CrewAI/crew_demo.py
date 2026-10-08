from crewai import Agent, Task, Crew, LLM
from crewai.tools import tool
import subprocess
import difflib
import os
import re
 
REPO_DIR = os.path.dirname(os.path.abspath(__file__))
 
 
def _write_file_impl(filename: str, content: str) -> str:
    """Skriver hele indholdet af en fil i repoet, viser en diff mod den eksisterende
    version (hvis filen findes), og laver derefter et git-commit."""
    path = os.path.join(REPO_DIR, filename)
    old_content = ""
    if os.path.exists(path):
        with open(path, "r", encoding="utf-8") as f:
            old_content = f.read()
 
    diff = "\n".join(
        difflib.unified_diff(
            old_content.splitlines(),
            content.splitlines(),
            fromfile=f"a/{filename}",
            tofile=f"b/{filename}",
            lineterm="",
        )
    )
    print(f"\n--- Diff for {filename} ---\n{diff or '(ny fil)'}\n")
 
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
 
    subprocess.run(["git", "add", filename], cwd=REPO_DIR, check=True)
    msg = f"feat: opdater {filename} via CrewAI-agent"
    subprocess.run(["git", "commit", "-m", msg], cwd=REPO_DIR, check=False)
 
    return f"Skrev {filename} ({len(content)} tegn) og forsoegte et git-commit."
 
 
@tool("write_file")
def write_file(filename: str, content: str) -> str:
    """Skriver hele indholdet af en fil i repoet, viser en diff mod den eksisterende
    version (hvis filen findes), og laver derefter et git-commit.
    Input: filename (relativ filsti, fx 'todo.py') og content (filens FULDE nye indhold).
    NB: bruges ikke laengere af udvikler/tester-agenterne i dette script (se extract_code
    nedenfor) - den staar tilbage her, hvis man senere vil koere med en model der
    faktisk understoetter native tool-calling i Ollama."""
    return _write_file_impl(filename, content)
 
 
def extract_code(raw_text: str) -> str:
    """Finder det STOERSTE fenced code-block (```python ... ``` eller ``` ... ```) i
    modellens raa svar - ikke det sidste. Lokale 7B-modeller svarer ofte med den
    egentlige implementering i foerste kodeblok, efterfulgt af et kort eksempel-snippet
    i et andet kodeblok ("her er hvordan du bruger koden") - det korte eksempel skal
    ikke overskrive den faktiske implementering.
    Bruges til at oversaette agent-output til filindhold UDEN at kraeve at den
    underliggende Ollama-model stoetter native tool-calling - mange lokale
    code-modeller (fx deepseek-coder:6.7b) svarer med en 400 fra Ollama
    ("does not support tools") hvis man giver dem et 'tools'-parameter."""
    blocks = re.findall(r"```(?:python)?\n(.*?)```", raw_text, re.DOTALL)
    if not blocks:
        return raw_text.strip()
    return max(blocks, key=len).strip()
 
 
def _check_syntax(filename: str, content: str) -> None:
    """Simpel sanity-check: er det udtrukne kodeblok overhovedet gyldig Python?
    Fejl her er ogsaa data (jf. ADR'en) - vi stopper ikke koersel, men flager det
    tydeligt i output."""
    try:
        compile(content, filename, "exec")
    except SyntaxError as e:
        print(f"\n!!! ADVARSEL: {filename} er IKKE gyldig Python-syntaks: {e} !!!\n")
 
 
architect_llm = LLM(model="ollama/qwen3:8b", base_url="http://127.0.0.1:11434")
dev_llm = LLM(model="ollama/deepseek-coder:6.7b", base_url="http://127.0.0.1:11435")
 
architect = Agent(
    role="Arkitekt",
    goal="Beskriv komponenter og interface-kontrakter for 'marker opgave som faerdig + filtrer paa status'",
    backstory="Erfaren softwarearkitekt der taenker i moduler og metodesignaturer.",
    llm=architect_llm,
)
 
developer = Agent(
    role="Udvikler",
    goal="Implementer featuren i todo.py baseret paa arkitektens plan",
    backstory=(
        "Pragmatisk Python-udvikler der skriver sin fulde loesning som ét samlet "
        "Python-kodeblok (```python ... ```) i sit svar - ikke andet tekst udenom "
        "selve kodeblokken, bortset fra evt. en kort forklaring foer blokken."
    ),
    llm=dev_llm,
)
 
tester = Agent(
    role="Tester",
    goal="Skriv simple tests for den nye kode",
    backstory=(
        "QA-ingenioer der skriver smaa, konkrete tests som ét samlet Python-kodeblok "
        "(```python ... ```) i sit svar - ikke andet tekst udenom selve kodeblokken, "
        "bortset fra evt. en kort forklaring foer blokken."
    ),
    llm=dev_llm,
)
 
architecture_task = Task(
    description=(
        "Beskriv komponent-opdeling og interface-kontrakter (klassenavne, metodenavne, "
        "signaturer) for featuren 'marker opgave som faerdig + filtrer paa status' i den "
        "eksisterende to-do-liste (TaskItem, TaskList, add_task)."
    ),
    expected_output="En kort tekst med klassenavne og metodesignaturer.",
    agent=architect,
)
 
implementation_task = Task(
    description=(
        "Implementer featuren i todo.py ud fra arkitektens plan. Hvis filen todo.py "
        "allerede findes i mappen, laes den og byg videre paa den eksisterende kode. "
        "Hvis den ikke findes, opret den fra grunden med de klasser arkitekten "
        "beskrev (TaskItem, TaskList, add_task). Returner HELE filens nye indhold som "
        "ét samlet Python-kodeblok (```python ... ```)."
    ),
    expected_output="Et fenced Python-kodeblok med hele indholdet af todo.py.",
    agent=developer,
    context=[architecture_task],
)
 
test_task = Task(
    description=(
        "Skriv simple pytest-tests for den nye todo.py-kode (test mark_complete og "
        "filter_by_status). Koden skal importere fra modulet 'todo' - altsaa "
        "'from todo import ...' - IKKE et placeholder-modulnavn som 'your_todo_file'. "
        "Brug de faktiske klasse- og metodenavne som findes i todo.py, ikke dem fra "
        "arkitektens oprindelige forslag, hvis udvikleren har navngivet dem anderledes. "
        "Returner HELE testfilens indhold som ét samlet Python-kodeblok (```python ... ```)."
    ),
    expected_output="Et fenced Python-kodeblok med hele indholdet af test_todo.py.",
    agent=tester,
    context=[implementation_task],
)
 
crew = Crew(
    agents=[architect, developer, tester],
    tasks=[architecture_task, implementation_task, test_task],
)
result = crew.kickoff()
 
# Oversaet agent-output til filaendringer selv, i stedet for at kraeve at Ollama-modellen
# stoetter native tool-calling (jf. ADR-001's begrundelse om selv at styre denne oversaettelse).
dev_code = extract_code(implementation_task.output.raw)
_check_syntax("todo.py", dev_code)
_write_file_impl("todo.py", dev_code)
 
test_code = extract_code(test_task.output.raw)
_check_syntax("test_todo.py", test_code)
_write_file_impl("test_todo.py", test_code)
 
print(result)