# SNEK_Iluvatar

A comprehensive advanced installer software solution consisting of two integrated applications: the End-User Installer and the Developer Packaging Tool.

## Overview

SNEK_Iluvatar is part of the SNEK initiative, providing a state-of-the-art installation platform with custom encryption, compression, and multi-threaded architecture for secure and efficient software distribution.

## Applications

### 1. End-User Installer
A visually stunning GUI installer with modern UX, supporting multiple installation modes, prerequisite checking, rollback, and multi-language support.

### 2. Developer Packaging Tool
A powerful tool for developers to create installation packages, manage dependencies, handle version control, and generate uninstallers.

## Architecture
SNEK_Iluvatar uses a single-executable distribution model. The **Developer Tool** embeds the package data directly into the footer of the **Installer GUI** executable. This creates a completely standalone installer that doesn't require any external files.

## Features
- **Standalone Installer**: Generates a single `.exe` file containing everything needed.
- **Embedded Data**: Uses a custom footer embedding mechanism (Metadata Offset + SPKG Magic).
- **Compression**: Built-in GZip compression for smaller installer sizes.
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
