
# 🤖 CmAI: AI-Powered CLI Command Executor

**CmAI** (Command AI) is a robust, cross-platform Command Line Interface (CLI) tool built with .NET 8. It leverages the **Google Gemini API** to translate natural language requests into executable shell commands (Bash, PowerShell, or CMD).

Designed for developers and power users, CmAI prioritizes safety by analyzing the intent of commands and requiring explicit confirmation for sensitive actions (like deletions or system modifications).

-----

## ✨ Features

  * **🗣️ Natural Language Processing:** Converts requests like *"Find all PDF files larger than 10MB"* into precise shell syntax.
  * **🛡️ Sensitive Command Guard:** Automatically flags dangerous operations (e.g., `rm -rf`, `Delete-Item`) and halts execution until you provide explicit confirmation.
  * **🖥️ Auto-OS Detection:** Detects whether it is running on Windows, Linux, or macOS and generates the appropriate command syntax (PowerShell vs. Bash).
  * **📝 Structured Logging:** Features enterprise-grade logging via **Serilog**, saving execution history to local files while keeping the console output clean.
  * **🛑 Graceful Cancellation:** Full support for `Ctrl+C` cancellation, safely aborting both API calls and command execution.
  * **🔒 Secure Configuration:** API keys are managed via Environment Variables, ensuring no secrets are stored in plain text or source control.

-----

## 🛠️ Technology Stack

  * **.NET 8.0** (Generic Host)
  * **Google.GenAI SDK** (Gemini 2.5 Flash Model)
  * **CommandLineParser** (Argument parsing)
  * **Serilog** (Structured file & console logging)
  * **Microsoft.Extensions.Hosting** (DI & Configuration)

-----

## 🚀 Installation & Setup

### 1\. Prerequisites

  * A **Google Gemini API Key** (Get one from [Google AI Studio](https://aistudio.google.com/)).
  * **.NET 8 SDK** (Only required to build; not required to run the published executable).

### 2\. Build for Production

To create a standalone executable that doesn't require .NET to be installed on the target machine:

```bash
# For Windows
dotnet publish -c Release --self-contained true -r win-x64

# For Linux
dotnet publish -c Release --self-contained true -r linux-x64

# For macOS
dotnet publish -c Release --self-contained true -r osx-x64
```

### 3\. Deployment

1.  Navigate to the publish folder (e.g., `bin\Release\net8.0\win-x64\publish\`).
2.  Copy all files to a permanent location (e.g., `C:\Tools\CmAI`).
3.  **Add this location to your System PATH** environment variable so you can run `cmai` from anywhere.

-----

## 🔑 Configuration (Required)

For security reasons, CmAI **does not** store the API key in configuration files. You must set it as an Environment Variable.

### Windows (PowerShell / CMD)

```powershell
setx GeminiAI_ApiKey "YOUR_ACTUAL_API_KEY"
```

*(Restart your terminal after running this command)*

### Linux / macOS (Bash / Zsh)

Add this to your `.bashrc` or `.zshrc`:

```bash
export GeminiAI_ApiKey="YOUR_ACTUAL_API_KEY"
```

-----

## 💻 Usage

The application uses a simple flag syntax.

| Flag | Alias | Description | Required? |
| :--- | :--- | :--- | :--- |
| `--query` | `-q` | The natural language instruction. **Must be wrapped in double quotes.** | ✅ Yes |
| `--help` | `-h` | Shows the help screen. | ❌ No |

### Examples

**1. Basic Information Retrieval**

```bash
cmai -q "Check the version of .NET installed"
```

**2. File Operations (Windows)**

```bash
cmai -q "Create a folder named Projects on the Desktop"
```

**3. Sensitive Command (Safety Check)**

```bash
cmai -q "Delete all log files in the current folder"
```

**Sample Output:**

```text
[INFO] Processing user request...
[INFO] AI Response:
  Command: Get-ChildItem *.log | Remove-Item -Force
  Conclusion: This command will permanently delete all files ending in .log in the current directory. WARNING: Irreversible.
  Sensitive: True

🚨 WARNING: This command is sensitive.
Please confirm again, press: y/n
> 
```

-----

## ❓ Troubleshooting

**"Parsing Error: Did you forget to wrap your query in quotes?"**

  * **Cause:** You ran `cmai -q list files` instead of `cmai -q "list files"`.
  * **Fix:** The shell splits words into separate arguments. Always wrap your query string in double quotes `"..."`.

**"System.IO.FileNotFoundException: appsettings.json"**

  * **Cause:** The application cannot find its configuration file because it is looking in the current folder instead of the installation folder.
  * **Fix:** Ensure you deployed the *entire* contents of the `publish` folder, including `appsettings.json`. The application is built to automatically resolve the correct path.

-----

## 📜 License

This project is open-source and available under the GNU General Public License v3.0
