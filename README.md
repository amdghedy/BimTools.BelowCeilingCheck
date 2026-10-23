# BIM TOOLS — Below Ceiling Check

**A Revit add-in for MEP coordination and model review.**

Below Ceiling Check helps BIM and MEP teams identify model elements located below the ceiling so they can review potential coordination concerns directly in their Revit workflow.

<p align="center">
  <strong>Revit 2020–2027</strong> · <strong>Windows</strong> · <strong>C# / .NET</strong>
</p>

---

## Complete feature and options reference

Below Ceiling Check combines model analysis, configurable filters, visual verification, review tracking, and report export in one modeless Revit window.

### 1. Ceiling reference and check scope

- **Ceiling elements (host and links):** use ceiling geometry in the host model and selected loaded linked models as the ceiling reference.
- **Fixed height above level:** use a user-defined ceiling/reference height above each level when ceiling elements are unavailable or a fixed datum is preferred.
- **Scope — Entire model:** check eligible elements across the project; linked MEP checking is supported in this scope.
- **Scope — Active view only:** limit the check to elements visible/eligible in the active view.
- **Scope — Current selection:** check the elements selected in Revit.
- **Display units:** millimeters, centimeters, meters, feet, or inches.
- **Tolerance:** configure the vertical tolerance used to ignore very small below-ceiling differences.
- **Minimum headroom check:** optionally flag overhead elements that reduce clearance above the floor; configure the minimum required headroom.
- **Section box / zoom padding:** set the surrounding margin used when zooming or framing a review area.
- **Ignore keywords:** skip elements when their category, family, type, or system contains a configured keyword. Separate terms with semicolons or commas.
- **Reset to defaults:** restore the standard check settings.
- **Saved settings:** settings are handled through the add-in settings service and reused by the tool.

### 2. Category, level, and model filters

- **Categories tab:** select individual categories to include; use **All**, **None**, or **Defaults** for quick selection.
- **Levels tab:** select specific ceiling levels; use **All** or **None**.
- **Host model controls:** independently enable host-model ceilings and host-model MEP elements.
- **Linked-model controls:** see each Revit link, its loaded status, and choose whether it supplies ceiling references, MEP elements, or both.
- Link roles can be suggested from the link name; the check uses the roles selected by the user.
- The available category list is grouped for easier navigation.

### 3. Results table, search, and review

Each finding can include the element ID, category, family, type, system, level, ceiling name and ID, ceiling source model, number of ceilings below, below-ceiling extent, depth below ceiling, bottom elevation above finished floor (AFF), ceiling AFF, headroom status, review status, and notes.

- **Severity filter:** show All, Critical, Warning, or Headroom findings.
- **Category filter:** narrow results to a selected category.
- **Search:** search the result set using the available result text.
- **Hide reviewed:** focus on findings that still need review.
- **Reviewed checkbox / Mark reviewed:** track completed reviews.
- **Notes:** add notes to individual findings.
- **Sort and multi-select:** use the results grid to sort columns and select multiple findings.
- **Visible count:** see how many results remain visible after filters.
- **Copy element IDs:** copy IDs from selected rows.
- **Row interaction:** double-click or select a row to access the associated model element; automatic selection and zoom can be enabled in Settings.

### 4. Revit visual investigation and view tools

- **Visual check:** select the element and the ceiling(s) it is below, colour them, and zoom to the relevant area.
- **Select:** select the host-model elements in Revit.
- **Zoom:** zoom the active view to the selected finding(s).
- **Isolate / Reset isolate:** temporarily isolate elements in the active view and restore the previous view state.
- **Colour / Reset colour:** apply or clear view graphic overrides; critical findings use red and warnings use orange.
- **Section box:** apply a section box in an active 3D view using the configured padding.
- **Review 3D view:** create a coloured 3D review view with a section box.
- **Create 3D check view:** create a focused view of a result element and its ceiling(s).
- **Visual check, isolated:** focus the view on the host and linked element/ceiling pair for targeted inspection.
- **Highlight and clear highlight:** highlight the relevant objects and clear temporary highlighting/isolation.
- **Linked-element markers:** optionally create temporary Generic Model DirectShape markers to represent linked elements and ceilings in view operations. Use **Clear highlight** to remove these temporary markers.
- **Reload model:** refresh the model information and link list in the open tool.
- **Cancel:** request cancellation of an active check.

Visual/view operations depend on the active Revit view and the available model geometry. Review findings in context before design or construction decisions.

### 5. Comments and report outputs

- **Write comments:** write a `[BCC]` note to the Revit element's Comments parameter for traceability.
- **Clear comments:** remove the tool's comments.
- **Export CSV:** export findings to a UTF-8 CSV that can be opened in Excel, including severity, source, IDs, category, family/type, system, level, ceiling data, elevations, depth, headroom, reviewed status, and notes.
- **HTML report:** generate a self-contained, print-ready HTML coordination report with project metadata, run date, reference mode, scope, tolerance, headroom setting, runtime, KPI counts, breakdowns by category/level/system, and the findings table.
- **Report metadata:** configure the company name and prepared-by fields in Settings.
- **Spreadsheet safety:** CSV text fields are escaped to reduce formula-injection risk.

### 6. Quick workflow

1. Open a Revit project and launch **BIM TOOLS — Below Ceiling Check**.
2. Choose the ceiling reference mode, scope, units, tolerance, and optional headroom criteria.
3. Select the categories and levels to include.
4. Configure host and linked-model roles.
5. Click **Run check** and monitor the results.
6. Filter/search findings, inspect them using visual check or the 3D review tools, and add notes.
7. Mark reviewed items and optionally write `[BCC]` comments to Revit.
8. Export a CSV for spreadsheet analysis or an HTML report for coordination review.

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
