# Build DocuMind Ollama Setup EXE

Build the standalone Windows setup utility from the repository root:

```powershell
dotnet publish .\DocuMind.Bootstrapper\DocuMind.Bootstrapper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\artifacts\DocuMind-Ollama-Setup
```

Distribute `DocuMind-Ollama-Setup.exe` together with the Word guide. The program needs Internet access while it installs Ollama and downloads the selected models. It writes `DocuMind-Ollama-Settings.txt` next to the EXE after hardware detection, so the user can copy the exact model names into DocuMind.

For a harmless survey-only verification, run:

```powershell
.\artifacts\DocuMind-Ollama-Setup\DocuMind-Ollama-Setup.exe --dry-run
```

The executable installs the current stable Ollama release using the official Windows installation script, sets `OLLAMA_MODELS` only for the current Windows user, and uses the local API at `http://localhost:11434`. It does not open the API to the network or collect hardware details remotely.
