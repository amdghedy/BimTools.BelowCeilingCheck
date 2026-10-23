# BIM TOOLS — Below Ceiling Check

**A Revit add-in for MEP coordination and model review.**

Below Ceiling Check helps BIM and MEP teams identify model elements located below the ceiling so they can review potential coordination concerns directly in their Revit workflow.

<p align="center">
  <strong>Revit 2020–2027</strong> · <strong>Windows</strong> · <strong>C# / .NET</strong>
</p>

---

## What it does

- Supports review of model elements positioned below the ceiling.
- Helps teams investigate potential ceiling-space and MEP coordination issues.
- Provides a focused utility for BIM coordination and quality-check workflows.
- Is built separately for each supported Revit version to match its API and runtime.

> **Important:** Treat results as coordination indicators. Review each finding in the project context before making design or construction decisions.

## Revit compatibility

| Revit version | Target runtime |
|---|---|
| 2020–2024 | .NET Framework 4.8 |
| 2025–2026 | .NET 8 |
| 2027 | .NET 10 |

The repository includes a GitHub Actions build matrix for all eight versions.

## Install a compiled version

**Recommended for most users:** use the package matching your installed Revit version.

1. Open the [Revit Versions folder](https://github.com/amdghedy/BimTools.BelowCeilingCheck/tree/main/Revit%20Versions).
2. Open the folder for your Revit release, for example **Revit 2025**.
3. Download that folder's files using GitHub's download options, or download the repository as a ZIP and locate the version folder.
4. Close Revit.
5. Keep the package structure intact, including the `BimTools.BelowCeilingCheck` folder and its DLL.
6. Run `Install-BelowCeilingCheck.ps1` from the package folder in PowerShell.
7. Start Revit again.

The installer copies the add-in into the current Windows user's Revit Addins directory under `%APPDATA%\Autodesk\Revit\Addins\<version>`. It does not require an MSI installer or administrator rights under a standard per-user setup.

If PowerShell's execution policy blocks the script, open PowerShell in the package folder and run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Install-BelowCeilingCheck.ps1"
```

Only run scripts you have reviewed and trust. To uninstall, close Revit and remove `BelowCeilingCheck.addin` and the `BimTools.BelowCeilingCheck` folder from that same version-specific Addins directory.

## Build from source

### Requirements

- Windows
- Git
- .NET SDK 8 and .NET SDK 10
- A compatible Revit API package restore through NuGet

### Build one Revit version

From the repository root, run the command for the required version:

```powershell
dotnet build .\BimTools.BelowCeilingCheck.csproj --configuration Release -p:RevitVersion=2025 -p:DeployAddin=false
```

Replace `2025` with a supported version from 2020 to 2027. The project targets .NET Framework 4.8 for Revit 2020–2024, .NET 8 for Revit 2025–2026, and .NET 10 for Revit 2027.

The default build is **compile-only**. It does not automatically copy files into a local Revit installation.

## Automated builds and packages

The workflow in [GitHub Actions](https://github.com/amdghedy/BimTools.BelowCeilingCheck/actions/workflows/build-revit-matrix.yml):

- Builds the add-in against Revit 2020, 2021, 2022, 2023, 2024, 2025, 2026, and 2027.
- Uploads version-specific build artifacts.
- On pushes to `main` and manual workflow runs, prepares the version folders with the compiled DLL, manifest, installer, and installation notes.

Pull requests run the build matrix but do not publish compiled packages to the repository.

## Repository layout

```text
.
├── .github/workflows/
│   └── build-revit-matrix.yml
├── BimTools.BelowCeilingCheck.csproj
├── BelowCeilingCheck.addin
└── Revit Versions/
    ├── Revit 2020/
    ├── Revit 2021/
    ├── ...
    └── Revit 2027/
```

Each published version folder is intended to contain:

- `BimTools.BelowCeilingCheck/BimTools.BelowCeilingCheck.dll`
- `BelowCeilingCheck.addin`
- `Install-BelowCeilingCheck.ps1`
- `README-INSTALL.txt`

## Troubleshooting

- **The add-in does not appear:** confirm that you installed the package matching your Revit version and that the manifest is in that version's Addins directory.
- **Revit reports a missing assembly:** check that the DLL remains in the `BimTools.BelowCeilingCheck` subfolder beside the manifest's expected relative path.
- **Revit was open during installation:** close and restart Revit after installing.
- **The version folder is missing:** check the [latest GitHub Actions run](https://github.com/amdghedy/BimTools.BelowCeilingCheck/actions/workflows/build-revit-matrix.yml) and confirm that both the build matrix and publish job succeeded.

## Contributing

Bug reports, reproducible examples, and focused improvements are welcome. Include your Revit version, the steps to reproduce the issue, and any relevant error message.

---

**BIM TOOLS — practical utilities for better BIM delivery and coordination.**
