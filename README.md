# SNEK Iluvatar

Currently rewriting the README and working on proper code documentation. I will also move towards replacing any and all AI generated code, which appeared due to my laziness at times.

SNEK Iluvatar is a installer software made by The SNEK initiative as a easy to set up packaging tool for developers and  good looking installer for users of their apps.

The software attempts to provide a installation platform with AES encryption, GZip compression, and multi threading for secure and efficient software distribution.

## Applications

### 1. End user Installer
A GUI installer with a modern UX, supporting multiple installation modes, prerequisite checking, rollback, and in the future multi language support.

### 2. Developer Packaging Tool
A tool for developers to create installation packages, manage dependencies and handle version control.

### 3. Uninstaller software
Auto packaged with every software, a tiny NASM written tool for quick user side uninstalling, currently still in development, but functioning in its current state.

## Architecture
SNEK Iluvatar uses a single executable distribution model. The Developer Tool embeds the package data directly into the footer of the Installer GUI executable. This creates a standalone installer that does not require any additional external files.

## Features
- **Standalone Installer**: The Developer Tool generates a single `.exe` file containing everything needed.
- **Embedded Data**: Uses a custom footer embedding mechanism via Metadata Offset combined with SPKG Magic.
- **Compression**: Built-in GZip compression for smaller installer sizes. Note that depending on the project the encryption percentage may vary.
- **Encryption**: Optional AES-256 encryption for package data.
- **Modern UI**: WPF-based interface for both developer and end-user tools.

## Building
To build the entire solution:
```bash
scripts\build.bat
```
Or manually using dotnet:
```bash
dotnet build SNEK_Iluvatar.sln -c Release
```

## Usage
1. Run `DeveloperTool.exe`.
2. Add the files you want to include in your installer.
3. Configure name, version, and optional compression/encryption.
4. Click **Build** to generate your standalone `installer.exe`.
